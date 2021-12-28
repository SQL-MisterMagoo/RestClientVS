﻿using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;

namespace BaseClasses
{
    internal abstract class TokenQuickInfoBase : IAsyncQuickInfoSourceProvider
    {
        [Import] internal IBufferTagAggregatorFactoryService _bufferTagAggregator = null;

        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer buffer)
        {
            ITagAggregator<TokenTag> tags = _bufferTagAggregator.CreateTagAggregator<TokenTag>(buffer);
            return buffer.Properties.GetOrCreateSingletonProperty(() => new TokenQuickInfo(tags));
        }
    }

    internal sealed class TokenQuickInfo : IAsyncQuickInfoSource
    {
        private readonly ITextBuffer _buffer;
        private readonly ITagAggregator<TokenTag> _tags;

        public TokenQuickInfo(ITagAggregator<TokenTag> tags)
        {
            _buffer = tags.BufferGraph.TopBuffer;
            _tags = tags;
        }

        public async Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
        {
            SnapshotPoint? triggerPoint = session.GetTriggerPoint(_buffer.CurrentSnapshot);

            if (triggerPoint.HasValue)
            {
                var span = new SnapshotSpan(triggerPoint.Value.Snapshot, triggerPoint.Value.Position, 0);
                IMappingTagSpan<TokenTag> tag = _tags.GetTags(span).FirstOrDefault(t => t.Tag.GetTooltipAsync != null);

                if (tag != null)
                {
                    var tooltip = await tag.Tag.GetTooltipAsync(triggerPoint.Value);

                    if (tooltip == null)
                    {
                        return null;
                    }

                    var container = new ContainerElement(ContainerElementStyle.Stacked, tooltip);
                    ITrackingSpan applicapleTo = _buffer.CurrentSnapshot.CreateTrackingSpan(tag.Span.GetSpans(_buffer)[0], SpanTrackingMode.EdgeExclusive);

                    return new QuickInfoItem(applicapleTo, container);
                }
            }

            return null;
        }

        public void Dispose()
        {
            // This provider does not perform any cleanup.
        }
    }
}
