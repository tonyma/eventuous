using System.Diagnostics;
using Eventuous.Diagnostics;
using Eventuous.Subscriptions.Context;

namespace Eventuous.Subscriptions.Diagnostics;

public static class SubscriptionActivity {
    public static Activity? Create(
        string                                      operation,
        IMessageConsumeContext                      context,
        IEnumerable<KeyValuePair<string, object?>>? tags = null
    ) {
        context.ParentContext ??= GetParentContext(context.Metadata);
        var activity = Create(operation, context.ParentContext, tags);
        return activity?.SetContextTags(context);
    }

    public static Activity? Start(
        string                                      operation,
        IMessageConsumeContext                      context,
        IEnumerable<KeyValuePair<string, object?>>? tags = null
    )
        => Create(operation, context, tags)?.Start();

    public static Activity? SetContextTags(this Activity? activity, IMessageConsumeContext context) {
        if (activity is not { IsAllDataRequested: true }) return activity;

        return activity
            .SetTag(TelemetryTags.Message.Type, context.MessageType)
            .SetTag(TelemetryTags.Message.Id, context.MessageId)
            .SetTag(TelemetryTags.Messaging.MessageId, context.MessageId)
            .SetTag(TelemetryTags.Eventuous.Stream, context.Stream)
            .SetTag(TelemetryTags.Eventuous.Subscription, context.SubscriptionId)
            .CopyParentTag(TelemetryTags.Messaging.ConversationId)
            .SetOrCopyParentTag(
                TelemetryTags.Messaging.CorrelationId,
                context.Metadata?.GetCorrelationId()
            );
    }

    static ActivityContext? GetParentContext(Metadata? metadata) {
        var tracingData = metadata?.GetTracingMeta();
        return tracingData?.ToActivityContext(true);
    }

    public static Activity? Create(
        string                                      operation,
        ActivityContext?                            parentContext = null,
        IEnumerable<KeyValuePair<string, object?>>? tags          = null
    )
        => EventuousDiagnostics.ActivitySource.CreateActivity(
            operation,
            ActivityKind.Consumer,
            parentContext ?? default,
            tags,
            idFormat: ActivityIdFormat.W3C
        );
}