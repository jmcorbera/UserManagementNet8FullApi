using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using UserManagement.Application.Common.Abstractions;
using UserManagement.Application.Common.Options;
using UserManagement.Domain.Entities;

namespace UserManagement.Application.Common.Behaviors;

public sealed class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse> where TResponse : class
{
    private readonly IIdempotencyRepository _idempotencyRepository;
    private readonly ILogger<IdempotencyBehavior<TRequest, TResponse>> _logger;
    private readonly IdempotencyOptions _options;

    public IdempotencyBehavior(IIdempotencyRepository idempotencyRepository, ILogger<IdempotencyBehavior<TRequest, TResponse>> logger, IOptions<IdempotencyOptions> options)
    {
        _idempotencyRepository = idempotencyRepository;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not IIdempotentCommand idempotentCommand)
            return await next();

        var key = idempotentCommand.IdempotencyKey;
        var commandName = typeof(TRequest).Name;
        var ttl = TimeSpan.FromHours(_options.TtlHours);

        // Try to create the record first (insert-first pattern).
        var newEntry = IdempotencyKey.Create(key, commandName);
        var created = await _idempotencyRepository.TryCreateAsync(newEntry, cancellationToken);

        if (!created)
        {
            //  Already exists → verify status
            var existingKey = await _idempotencyRepository.GetByKeyAsync(key, cancellationToken);

            // This should not happen if there is a unique index    
            if (existingKey is null)
                throw new InvalidOperationException($"Idempotency key {key} exists but could not be retrieved.");

            // Optional: you could delete it and retry
            if (existingKey.IsExpired(ttl))
            {
                _logger.LogWarning("Idempotency key {Key} expired for command {Command}", key, commandName);
                throw new InvalidOperationException("Idempotency key expired.");
            }

            switch (existingKey.Status)
            {
                case IdempotencyStatus.Completed:
                    if (!string.IsNullOrWhiteSpace(existingKey.Result))
                    {
                        _logger.LogInformation("Returning cached result for Idempotency key {Key} already completed for command {Command}", key, commandName);
                        var cachedResponse = JsonSerializer.Deserialize<TResponse>(existingKey.Result);

                        if (cachedResponse is not null)
                            return cachedResponse;
                    }
                    break;
                case IdempotencyStatus.InProgress:
                    _logger.LogWarning("Request with key {Key} is already in progress", key);
                    throw new InvalidOperationException("A request with the same IdempotencyKey is already being processed.");

                case IdempotencyStatus.Failed:
                    _logger.LogWarning("Previous request with idempotency key {Key} for command {CommandName} failed", key, commandName);
                    break;
            }
        }

        try
        {
            var response = await next();

            // save the result as completed
            var resultJson = JsonSerializer.Serialize(response);
            await _idempotencyRepository.MarkAsCompletedAsync(key, resultJson, cancellationToken);

            _logger.LogInformation("Stored result for idempotency key {Key} and command {CommandName}", key, commandName);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing idempotent command {CommandName} with key {Key}", commandName, key);

            // Mark as failed
            await _idempotencyRepository.MarkAsFailedAsync(key, cancellationToken);
            throw;
        }
    }
}
