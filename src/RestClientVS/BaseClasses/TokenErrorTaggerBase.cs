using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;

namespace BaseClasses
{
    public abstract class TokenErrorTaggerBase : ITaggerProvider
    {
        [Import] internal IBufferTagAggregatorFactoryService _bufferTagAggregator = null;

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            ITagAggregator<TokenTag> tags = _bufferTagAggregator.CreateTagAggregator<TokenTag>(buffer);
            return buffer.Properties.GetOrCreateSingletonProperty(() => new ErrorTagger(tags)) as ITagger<T>;
        }
    }

    public class ErrorTagger : TokenTaggerBase<IErrorTag>
    {
        private readonly TableDataSource _dataSource;

        public ErrorTagger(ITagAggregator<TokenTag> tags) : base(tags)
        {
            _dataSource = new TableDataSource(tags.BufferGraph.TopBuffer.ContentType.DisplayName);
        }

        public override IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans, bool isFullParse)
        {
            IEnumerable<IMappingTagSpan<TokenTag>> tags = Tags.GetTags(spans).Where(t => !t.Tag.IsValid);

            foreach (IMappingTagSpan<TokenTag> tag in tags)
            {
                NormalizedSnapshotSpanCollection tagSpans = tag.Span.GetSpans(tag.Span.AnchorBuffer.CurrentSnapshot);
                var tooltip = string.Join(Environment.NewLine, tag.Tag.Errors);
                var errorTag = new ErrorTag(PredefinedErrorTypeNames.SyntaxError, tooltip);

                foreach (SnapshotSpan span in tagSpans)
                {
                    yield return new TagSpan<IErrorTag>(span, errorTag);
                }
            }

            if (isFullParse)
            {
                PopulateErrorList(tags);
            }
        }

        private void PopulateErrorList(IEnumerable<IMappingTagSpan<TokenTag>> tags)
        {
            IEnumerable<ErrorListItem> errors = tags.SelectMany(t => t.Tag.Errors);

            if (!errors.Any())
            {
                _dataSource.CleanAllErrors();
            }
            else
            {
                _dataSource.AddErrors(errors);
            }
        }

        public override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dataSource.CleanAllErrors();
            }

            base.Dispose(disposing);
        }
    }
}
