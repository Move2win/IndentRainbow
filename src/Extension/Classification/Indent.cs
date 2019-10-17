﻿using IndentRainbow.Extension.Drawing;
using IndentRainbow.Extension.Options;
using IndentRainbow.Logic.Classification;
using IndentRainbow.Logic.Colors;
using IndentRainbow.Logic.Drawing;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using System;

namespace IndentRainbow.Extension
{
    /// <summary>
    /// Decorates the text using all necessary components
    /// </summary>
    public sealed class Indent
    {
        /// <summary>
        /// The layer of the adornment.
        /// </summary>
        private readonly IAdornmentLayer layer;

        /// <summary>
        /// Text view where the adornment is created.
        /// </summary>
        private readonly IWpfTextView view;

        /// <summary>
        /// Background drawer which is used for drawing the rainbow effect
        /// </summary>
        private readonly IBackgroundTextIndexDrawer drawer;

        /// <summary>
        /// Color getter used for getting the correct color for an indentation level
        /// </summary>
        private readonly IRainbowBrushGetter colorGetter;

        /// <summary>
        /// Validator used for checking wether a given string is a valid indentation
        /// </summary>
        private readonly IIndentValidator validator;

        /// <summary>
        /// Decorator used for decorating a line
        /// </summary>
        private readonly ILineDecorator decorator;

        /// <summary>
        /// Initializes a new instance of the <see cref="Indent"/> class.
        /// </summary>
        /// <param name="view">Text view to create the adornment for</param>
        //Ignoring warning since this adornment is always on UI thread
#pragma warning disable VSTHRD010
        public Indent(IWpfTextView view, ITextDocumentFactoryService textDocumentFactory)
        {
            if (view == null)
            {
                throw new ArgumentNullException("view");
            }
            this.layer = view.GetAdornmentLayer("Indent");

            this.view = view;
            this.view.LayoutChanged += this.OnLayoutChanged;
            this.drawer = new BackgroundTextIndexDrawer(this.layer, this.view);

            this.colorGetter = new RainbowBrushGetter()
            {
                brushes = OptionsManager.brushes.Get(),
                errorColor = OptionsManager.errorBrush.Get()
            };
            this.validator = new IndentValidator(
                OptionsManager.indentSize.Get()
            );


            ITextDocument textDocument;
            var result = textDocumentFactory.TryGetTextDocument(this.view.TextBuffer, out textDocument);
            if (result)
            {
                var filePath = textDocument.FilePath;
                var filePathSplit = filePath.Split('.');
                var extension = filePathSplit[filePathSplit.Length - 1];
                if (OptionsManager.fileExtensionsDictionary.Get().ContainsKey(extension))
                {
                    this.validator = new IndentValidator(OptionsManager.fileExtensionsDictionary.Get()[extension]);
                }
            }

            if(OptionsManager.highlightingMode.Get() == HighlightingMode.Alternating)
            {
                this.decorator = new AlternatingLineDecorator(
                    this.drawer, this.colorGetter, this.validator)
                {
                    detectErrors = OptionsManager.detectErrors.Get()
                };
            }
            if(OptionsManager.highlightingMode.Get() == HighlightingMode.Monocolor)
            {
                this.decorator = new MonocolorLineDecorator(
                    this.drawer, this.colorGetter, this.validator)
                {
                    detectErrors = OptionsManager.detectErrors.Get()
                };
            }

        }

        /// <summary>
        /// Handles whenever the text displayed in the view changes by adding the adornment to any reformatted lines
        /// </summary>
        /// <remarks><para>This event is raised whenever the rendered text displayed in the <see cref="ITextView"/> changes.</para>
        /// <para>It is raised whenever the view does a layout (which happens when DisplayTextLineContainingBufferPosition is called or in response to text or classification changes).</para>
        /// <para>It is also raised whenever the view scrolls horizontally or when its size changes.</para>
        /// </remarks>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        internal void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            foreach (ITextViewLine line in e.NewOrReformattedLines)
            {
                this.CreateVisuals(line);
            }
        }

        /// <summary>
        /// Retrieves relevant information to pass it to the line decorator
        /// </summary>
        /// <param name="line">Line to add the adornments</param>
        private void CreateVisuals(ITextViewLine line)
        {
            int start = line.Start;
            int end = line.End;

            string text = line.Snapshot.GetText();
            this.decorator.DecorateLine(text, start, end);
        }
    }
}
