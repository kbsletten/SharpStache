using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace SharpStache.Test
{
    [TestClass]
    public class SpecTest
    {
        /*
Comment tags represent content that should never appear in the resulting
output.

The tag's content may contain any substring (including newlines) EXCEPT the
closing delimiter.

Comment tags SHOULD be treated as standalone when appropriate.

*/
        #region Comments
        /* Comment blocks should be removed from the template. */
        [TestMethod]
        public void TestCommentsInline()
        {
            string template = "12345{{! Comment Block! }}67890";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { };
            string expected = "1234567890";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Multiline comments should be permitted. */
        [TestMethod]
        public void TestCommentsMultiline()
        {
            string template = "12345{{!\n  This is a\n  multi-line comment...\n}}67890\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { };
            string expected = "1234567890\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* All standalone comment lines should be removed. */
        [TestMethod]
        public void TestCommentsStandalone()
        {
            string template = "Begin.\n{{! Comment Block! }}\nEnd.\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { };
            string expected = "Begin.\nEnd.\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* All standalone comment lines should be removed. */
        [TestMethod]
        public void TestCommentsIndentedStandalone()
        {
            string template = "Begin.\n  {{! Indented Comment Block! }}\nEnd.\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { };
            string expected = "Begin.\nEnd.\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* "\r\n" should be considered a newline for standalone tags. */
        [TestMethod]
        public void TestCommentsStandaloneLineEndings()
        {
            string template = "|\r\n{{! Standalone Comment }}\r\n|";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { };
            string expected = "|\r\n|";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Standalone tags should not require a newline to precede them. */
        [TestMethod]
        public void TestCommentsStandaloneWithoutPreviousLine()
        {
            string template = "  {{! I'm Still Standalone }}\n!";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { };
            string expected = "!";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Standalone tags should not require a newline to follow them. */
        [TestMethod]
        public void TestCommentsStandaloneWithoutNewline()
        {
            string template = "!\n  {{! I'm Still Standalone }}";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { };
            string expected = "!\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* All standalone comment lines should be removed. */
        [TestMethod]
        public void TestCommentsMultilineStandalone()
        {
            string template = "Begin.\n{{!\nSomething's going on here...\n}}\nEnd.\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { };
            string expected = "Begin.\nEnd.\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* All standalone comment lines should be removed. */
        [TestMethod]
        public void TestCommentsIndentedMultilineStandalone()
        {
            string template = "Begin.\n  {{!\n    Something's going on here...\n  }}\nEnd.\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { };
            string expected = "Begin.\nEnd.\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Inline comments should not strip whitespace */
        [TestMethod]
        public void TestCommentsIndentedInline()
        {
            string template = "  12 {{! 34 }}\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { };
            string expected = "  12 \n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Comment removal should preserve surrounding whitespace. */
        [TestMethod]
        public void TestCommentsSurroundingWhitespace()
        {
            string template = "12345 {{! Comment Block! }} 67890";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { };
            string expected = "12345  67890";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        #endregion
        /*
Set Delimiter tags are used to change the tag delimiters for all content
following the tag in the current compilation unit.

The tag's content MUST be any two non-whitespace sequences (separated by
whitespace) EXCEPT an equals sign ('=') followed by the current closing
delimiter.

Set Delimiter tags SHOULD be treated as standalone when appropriate.

*/
        #region Delimiters
        /* The equals sign (used on both sides) should permit delimiter changes. */
        [TestMethod]
        public void TestDelimitersPairBehavior()
        {
            string template = "{{=<% %>=}}(<%text%>)";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { text = "Hey!" };
            string expected = "(Hey!)";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Characters with special meaning regexen should be valid delimiters. */
        [TestMethod]
        public void TestDelimitersSpecialCharacters()
        {
            string template = "({{=[ ]=}}[text])";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { text = "It worked!" };
            string expected = "(It worked!)";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Delimiters set outside sections should persist. */
        [TestMethod]
        public void TestDelimitersSections()
        {
            string template = "[\n{{#section}}\n  {{data}}\n  |data|\n{{/section}}\n\n{{= | | =}}\n|#section|\n  {{data}}\n  |data|\n|/section|\n]\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { section = true, data = "I got interpolated." };
            string expected = "[\n  I got interpolated.\n  |data|\n\n  {{data}}\n  I got interpolated.\n]\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Delimiters set outside inverted sections should persist. */
        [TestMethod]
        public void TestDelimitersInvertedSections()
        {
            string template = "[\n{{^section}}\n  {{data}}\n  |data|\n{{/section}}\n\n{{= | | =}}\n|^section|\n  {{data}}\n  |data|\n|/section|\n]\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { section = false, data = "I got interpolated." };
            string expected = "[\n  I got interpolated.\n  |data|\n\n  {{data}}\n  I got interpolated.\n]\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Delimiters set in a parent template should not affect a partial. */
        [TestMethod]
        public void TestDelimitersPartialInheritence()
        {
            string template = "[ {{>include}} ]\n{{= | | =}}\n[ |>include| ]\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { { "include", ".{{value}}." } };
            object data = new { value = "yes" };
            string expected = "[ .yes. ]\n[ .yes. ]\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Delimiters set in a partial should not affect the parent template. */
        [TestMethod]
        public void TestDelimitersPostPartialBehavior()
        {
            string template = "[ {{>include}} ]\n[ .{{value}}.  .|value|. ]\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { { "include", ".{{value}}. {{= | | =}} .|value|." } };
            object data = new { value = "yes" };
            string expected = "[ .yes.  .yes. ]\n[ .yes.  .|value|. ]\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Surrounding whitespace should be left untouched. */
        [TestMethod]
        public void TestDelimitersSurroundingWhitespace()
        {
            string template = "| {{=@ @=}} |";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { };
            string expected = "|  |";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Whitespace should be left untouched. */
        [TestMethod]
        public void TestDelimitersOutlyingWhitespaceInline()
        {
            string template = " | {{=@ @=}}\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { };
            string expected = " | \n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Standalone lines should be removed from the template. */
        [TestMethod]
        public void TestDelimitersStandaloneTag()
        {
            string template = "Begin.\n{{=@ @=}}\nEnd.\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { };
            string expected = "Begin.\nEnd.\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Indented standalone lines should be removed from the template. */
        [TestMethod]
        public void TestDelimitersIndentedStandaloneTag()
        {
            string template = "Begin.\n  {{=@ @=}}\nEnd.\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { };
            string expected = "Begin.\nEnd.\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* "\r\n" should be considered a newline for standalone tags. */
        [TestMethod]
        public void TestDelimitersStandaloneLineEndings()
        {
            string template = "|\r\n{{= @ @ =}}\r\n|";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { };
            string expected = "|\r\n|";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Standalone tags should not require a newline to precede them. */
        [TestMethod]
        public void TestDelimitersStandaloneWithoutPreviousLine()
        {
            string template = "  {{=@ @=}}\n=";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { };
            string expected = "=";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Standalone tags should not require a newline to follow them. */
        [TestMethod]
        public void TestDelimitersStandaloneWithoutNewline()
        {
            string template = "=\n  {{=@ @=}}";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { };
            string expected = "=\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Superfluous in-tag whitespace should be ignored. */
        [TestMethod]
        public void TestDelimitersPairwithPadding()
        {
            string template = "|{{= @   @ =}}|";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { };
            string expected = "||";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        #endregion
        /*
Interpolation tags are used to integrate dynamic content into the template.

The tag's content MUST be a non-whitespace character sequence NOT containing
the current closing delimiter.

This tag's content names the data to replace the tag.  A single period (`.`)
indicates that the item currently sitting atop the context stack should be
used; otherwise, name resolution is as follows:
  1) Split the name on periods; the first part is the name to resolve, any
  remaining parts should be retained.
  2) Walk the context stack from top to bottom, finding the first context
  that is a) a hash containing the name as a key OR b) an object responding
  to a method with the given name.
  3) If the context is a hash, the data is the value associated with the
  name.
  4) If the context is an object, the data is the value returned by the
  method with the given name.
  5) If any name parts were retained in step 1, each should be resolved
  against a context stack containing only the result from the former
  resolution.  If any part fails resolution, the result should be considered
  falsey, and should interpolate as the empty string.
Data should be coerced into a string (and escaped, if appropriate) before
interpolation.

The Interpolation tags MUST NOT be treated as standalone.

*/
        #region Interpolation
        /* Mustache-free templates should render as-is. */
        [TestMethod]
        public void TestInterpolationNoInterpolation()
        {
            string template = "Hello from {Mustache}!\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { };
            string expected = "Hello from {Mustache}!\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Unadorned tags should interpolate content into the template. */
        [TestMethod]
        public void TestInterpolationBasicInterpolation()
        {
            string template = "Hello, {{subject}}!\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { subject = "world" };
            string expected = "Hello, world!\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Basic interpolation should be HTML escaped. */
        [TestMethod]
        public void TestInterpolationHTMLEscaping()
        {
            string template = "These characters should be HTML escaped: {{forbidden}}\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { forbidden = "& \" < >" };
            string expected = "These characters should be HTML escaped: & \" < >\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Triple mustaches should interpolate without HTML escaping. */
        [TestMethod]
        public void TestInterpolationTripleMustache()
        {
            string template = "These characters should not be HTML escaped: {{{forbidden}}}\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { forbidden = "& \" < >" };
            string expected = "These characters should not be HTML escaped: & \" < >\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Ampersand should interpolate without HTML escaping. */
        [TestMethod]
        public void TestInterpolationAmpersand()
        {
            string template = "These characters should not be HTML escaped: {{&forbidden}}\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { forbidden = "& \" < >" };
            string expected = "These characters should not be HTML escaped: & \" < >\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Integers should interpolate seamlessly. */
        [TestMethod]
        public void TestInterpolationBasicIntegerInterpolation()
        {
            string template = "\"{{mph}} miles an hour!\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { mph = 85 };
            string expected = "\"85 miles an hour!\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Integers should interpolate seamlessly. */
        [TestMethod]
        public void TestInterpolationTripleMustacheIntegerInterpolation()
        {
            string template = "\"{{{mph}}} miles an hour!\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { mph = 85 };
            string expected = "\"85 miles an hour!\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Integers should interpolate seamlessly. */
        [TestMethod]
        public void TestInterpolationAmpersandIntegerInterpolation()
        {
            string template = "\"{{&mph}} miles an hour!\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { mph = 85 };
            string expected = "\"85 miles an hour!\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Decimals should interpolate seamlessly with proper significance. */
        [TestMethod]
        public void TestInterpolationBasicDecimalInterpolation()
        {
            string template = "\"{{power}} jiggawatts!\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { power = 1.21 };
            string expected = "\"1.21 jiggawatts!\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Decimals should interpolate seamlessly with proper significance. */
        [TestMethod]
        public void TestInterpolationTripleMustacheDecimalInterpolation()
        {
            string template = "\"{{{power}}} jiggawatts!\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { power = 1.21 };
            string expected = "\"1.21 jiggawatts!\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Decimals should interpolate seamlessly with proper significance. */
        [TestMethod]
        public void TestInterpolationAmpersandDecimalInterpolation()
        {
            string template = "\"{{&power}} jiggawatts!\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { power = 1.21 };
            string expected = "\"1.21 jiggawatts!\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Failed context lookups should default to empty strings. */
        [TestMethod]
        public void TestInterpolationBasicContextMissInterpolation()
        {
            string template = "I ({{cannot}}) be seen!";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { };
            string expected = "I () be seen!";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Failed context lookups should default to empty strings. */
        [TestMethod]
        public void TestInterpolationTripleMustacheContextMissInterpolation()
        {
            string template = "I ({{{cannot}}}) be seen!";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { };
            string expected = "I () be seen!";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Failed context lookups should default to empty strings. */
        [TestMethod]
        public void TestInterpolationAmpersandContextMissInterpolation()
        {
            string template = "I ({{&cannot}}) be seen!";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { };
            string expected = "I () be seen!";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Dotted names should be considered a form of shorthand for sections. */
        [TestMethod]
        public void TestInterpolationDottedNamesBasicInterpolation()
        {
            string template = "\"{{person.name}}\" == \"{{#person}}{{name}}{{/person}}\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { person = new { name = "Joe" } };
            string expected = "\"Joe\" == \"Joe\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Dotted names should be considered a form of shorthand for sections. */
        [TestMethod]
        public void TestInterpolationDottedNamesTripleMustacheInterpolation()
        {
            string template = "\"{{{person.name}}}\" == \"{{#person}}{{{name}}}{{/person}}\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { person = new { name = "Joe" } };
            string expected = "\"Joe\" == \"Joe\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Dotted names should be considered a form of shorthand for sections. */
        [TestMethod]
        public void TestInterpolationDottedNamesAmpersandInterpolation()
        {
            string template = "\"{{&person.name}}\" == \"{{#person}}{{&name}}{{/person}}\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { person = new { name = "Joe" } };
            string expected = "\"Joe\" == \"Joe\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Dotted names should be functional to any level of nesting. */
        [TestMethod]
        public void TestInterpolationDottedNamesArbitraryDepth()
        {
            string template = "\"{{a.b.c.d.e.name}}\" == \"Phil\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { a = new { b = new { c = new { d = new { e = new { name = "Phil" } } } } } };
            string expected = "\"Phil\" == \"Phil\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Any falsey value prior to the last part of the name should yield ''. */
        [TestMethod]
        public void TestInterpolationDottedNamesBrokenChains()
        {
            string template = "\"{{a.b.c}}\" == \"\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { a = new { } };
            string expected = "\"\" == \"\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Each part of a dotted name should resolve only against its parent. */
        [TestMethod]
        public void TestInterpolationDottedNamesBrokenChainResolution()
        {
            string template = "\"{{a.b.c.name}}\" == \"\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { a = new { b = new { } }, c = new { name = "Jim" } };
            string expected = "\"\" == \"\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* The first part of a dotted name should resolve as any other name. */
        [TestMethod]
        public void TestInterpolationDottedNamesInitialResolution()
        {
            string template = "\"{{#a}}{{b.c.d.e.name}}{{/a}}\" == \"Phil\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { a = new { b = new { c = new { d = new { e = new { name = "Phil" } } } } }, b = new { c = new { d = new { e = new { name = "Wrong" } } } } };
            string expected = "\"Phil\" == \"Phil\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Interpolation should not alter surrounding whitespace. */
        [TestMethod]
        public void TestInterpolationInterpolationSurroundingWhitespace()
        {
            string template = "| {{string}} |";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { @string = "---" };
            string expected = "| --- |";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Interpolation should not alter surrounding whitespace. */
        [TestMethod]
        public void TestInterpolationTripleMustacheSurroundingWhitespace()
        {
            string template = "| {{{string}}} |";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { @string = "---" };
            string expected = "| --- |";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Interpolation should not alter surrounding whitespace. */
        [TestMethod]
        public void TestInterpolationAmpersandSurroundingWhitespace()
        {
            string template = "| {{&string}} |";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { @string = "---" };
            string expected = "| --- |";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Standalone interpolation should not alter surrounding whitespace. */
        [TestMethod]
        public void TestInterpolationInterpolationStandalone()
        {
            string template = "  {{string}}\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { @string = "---" };
            string expected = "  ---\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Standalone interpolation should not alter surrounding whitespace. */
        [TestMethod]
        public void TestInterpolationTripleMustacheStandalone()
        {
            string template = "  {{{string}}}\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { @string = "---" };
            string expected = "  ---\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Standalone interpolation should not alter surrounding whitespace. */
        [TestMethod]
        public void TestInterpolationAmpersandStandalone()
        {
            string template = "  {{&string}}\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { @string = "---" };
            string expected = "  ---\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Superfluous in-tag whitespace should be ignored. */
        [TestMethod]
        public void TestInterpolationInterpolationWithPadding()
        {
            string template = "|{{ string }}|";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { @string = "---" };
            string expected = "|---|";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Superfluous in-tag whitespace should be ignored. */
        [TestMethod]
        public void TestInterpolationTripleMustacheWithPadding()
        {
            string template = "|{{{ string }}}|";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { @string = "---" };
            string expected = "|---|";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Superfluous in-tag whitespace should be ignored. */
        [TestMethod]
        public void TestInterpolationAmpersandWithPadding()
        {
            string template = "|{{& string }}|";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { @string = "---" };
            string expected = "|---|";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        #endregion
        /*
Inverted Section tags and End Section tags are used in combination to wrap a
section of the template.

These tags' content MUST be a non-whitespace character sequence NOT
containing the current closing delimiter; each Inverted Section tag MUST be
followed by an End Section tag with the same content within the same
section.

This tag's content names the data to replace the tag.  Name resolution is as
follows:
  1) Split the name on periods; the first part is the name to resolve, any
  remaining parts should be retained.
  2) Walk the context stack from top to bottom, finding the first context
  that is a) a hash containing the name as a key OR b) an object responding
  to a method with the given name.
  3) If the context is a hash, the data is the value associated with the
  name.
  4) If the context is an object and the method with the given name has an
  arity of 1, the method SHOULD be called with a String containing the
  unprocessed contents of the sections; the data is the value returned.
  5) Otherwise, the data is the value returned by calling the method with
  the given name.
  6) If any name parts were retained in step 1, each should be resolved
  against a context stack containing only the result from the former
  resolution.  If any part fails resolution, the result should be considered
  falsey, and should interpolate as the empty string.
If the data is not of a list type, it is coerced into a list as follows: if
the data is truthy (e.g. `!!data == true`), use a single-element list
containing the data, otherwise use an empty list.

This section MUST NOT be rendered unless the data list is empty.

Inverted Section and End Section tags SHOULD be treated as standalone when
appropriate.

*/
        #region Inverted
        /* Falsey sections should have their contents rendered. */
        [TestMethod]
        public void TestInvertedFalsey()
        {
            string template = "\"{{^boolean}}This should be rendered.{{/boolean}}\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { boolean = false };
            string expected = "\"This should be rendered.\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Truthy sections should have their contents omitted. */
        [TestMethod]
        public void TestInvertedTruthy()
        {
            string template = "\"{{^boolean}}This should not be rendered.{{/boolean}}\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { boolean = true };
            string expected = "\"\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Objects and hashes should behave like truthy values. */
        [TestMethod]
        public void TestInvertedContext()
        {
            string template = "\"{{^context}}Hi {{name}}.{{/context}}\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { context = new { name = "Joe" } };
            string expected = "\"\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Lists should behave like truthy values. */
        [TestMethod]
        public void TestInvertedList()
        {
            string template = "\"{{^list}}{{n}}{{/list}}\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { list = new object[] { new { n = 1 }, new { n = 2 }, new { n = 3 } } };
            string expected = "\"\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Empty lists should behave like falsey values. */
        [TestMethod]
        public void TestInvertedEmptyList()
        {
            string template = "\"{{^list}}Yay lists!{{/list}}\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { list = new object[] { } };
            string expected = "\"Yay lists!\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Multiple inverted sections per template should be permitted. */
        [TestMethod]
        public void TestInvertedDoubled()
        {
            string template = "{{^bool}}\n* first\n{{/bool}}\n* {{two}}\n{{^bool}}\n* third\n{{/bool}}\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { two = "second", @bool = false };
            string expected = "* first\n* second\n* third\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Nested falsey sections should have their contents rendered. */
        [TestMethod]
        public void TestInvertedNestedFalsey()
        {
            string template = "| A {{^bool}}B {{^bool}}C{{/bool}} D{{/bool}} E |";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { @bool = false };
            string expected = "| A B C D E |";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Nested truthy sections should be omitted. */
        [TestMethod]
        public void TestInvertedNestedTruthy()
        {
            string template = "| A {{^bool}}B {{^bool}}C{{/bool}} D{{/bool}} E |";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { @bool = true };
            string expected = "| A  E |";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Failed context lookups should be considered falsey. */
        [TestMethod]
        public void TestInvertedContextMisses()
        {
            string template = "[{{^missing}}Cannot find key 'missing'!{{/missing}}]";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { };
            string expected = "[Cannot find key 'missing'!]";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Dotted names should be valid for Inverted Section tags. */
        [TestMethod]
        public void TestInvertedDottedNamesTruthy()
        {
            string template = "\"{{^a.b.c}}Not Here{{/a.b.c}}\" == \"\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { a = new { b = new { c = true } } };
            string expected = "\"\" == \"\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Dotted names should be valid for Inverted Section tags. */
        [TestMethod]
        public void TestInvertedDottedNamesFalsey()
        {
            string template = "\"{{^a.b.c}}Not Here{{/a.b.c}}\" == \"Not Here\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { a = new { b = new { c = false } } };
            string expected = "\"Not Here\" == \"Not Here\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Dotted names that cannot be resolved should be considered falsey. */
        [TestMethod]
        public void TestInvertedDottedNamesBrokenChains()
        {
            string template = "\"{{^a.b.c}}Not Here{{/a.b.c}}\" == \"Not Here\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { a = new { } };
            string expected = "\"Not Here\" == \"Not Here\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Inverted sections should not alter surrounding whitespace. */
        [TestMethod]
        public void TestInvertedSurroundingWhitespace()
        {
            string template = " | {{^boolean}}	|	{{/boolean}} | \n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { boolean = false };
            string expected = " | 	|	 | \n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Inverted should not alter internal whitespace. */
        [TestMethod]
        public void TestInvertedInternalWhitespace()
        {
            string template = " | {{^boolean}} {{! Important Whitespace }}\n {{/boolean}} | \n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { boolean = false };
            string expected = " |  \n  | \n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Single-line sections should not alter surrounding whitespace. */
        [TestMethod]
        public void TestInvertedIndentedInlineSections()
        {
            string template = " {{^boolean}}NO{{/boolean}}\n {{^boolean}}WAY{{/boolean}}\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { boolean = false };
            string expected = " NO\n WAY\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Standalone lines should be removed from the template. */
        [TestMethod]
        public void TestInvertedStandaloneLines()
        {
            string template = "| This Is\n{{^boolean}}\n|\n{{/boolean}}\n| A Line\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { boolean = false };
            string expected = "| This Is\n|\n| A Line\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Standalone indented lines should be removed from the template. */
        [TestMethod]
        public void TestInvertedStandaloneIndentedLines()
        {
            string template = "| This Is\n  {{^boolean}}\n|\n  {{/boolean}}\n| A Line\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { boolean = false };
            string expected = "| This Is\n|\n| A Line\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* "\r\n" should be considered a newline for standalone tags. */
        [TestMethod]
        public void TestInvertedStandaloneLineEndings()
        {
            string template = "|\r\n{{^boolean}}\r\n{{/boolean}}\r\n|";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { boolean = false };
            string expected = "|\r\n|";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Standalone tags should not require a newline to precede them. */
        [TestMethod]
        public void TestInvertedStandaloneWithoutPreviousLine()
        {
            string template = "  {{^boolean}}\n^{{/boolean}}\n/";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { boolean = false };
            string expected = "^\n/";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Standalone tags should not require a newline to follow them. */
        [TestMethod]
        public void TestInvertedStandaloneWithoutNewline()
        {
            string template = "^{{^boolean}}\n/\n  {{/boolean}}";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { boolean = false };
            string expected = "^\n/\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Superfluous in-tag whitespace should be ignored. */
        [TestMethod]
        public void TestInvertedPadding()
        {
            string template = "|{{^ boolean }}={{/ boolean }}|";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { boolean = false };
            string expected = "|=|";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        #endregion
        /*
Partial tags are used to expand an external template into the current
template.

The tag's content MUST be a non-whitespace character sequence NOT containing
the current closing delimiter.

This tag's content names the partial to inject.  Set Delimiter tags MUST NOT
affect the parsing of a partial.  The partial MUST be rendered against the
context stack local to the tag.  If the named partial cannot be found, the
empty string SHOULD be used instead, as in interpolations.

Partial tags SHOULD be treated as standalone when appropriate.  If this tag
is used standalone, any whitespace preceding the tag should treated as
indentation, and prepended to each line of the partial before rendering.

*/
        #region Partials
        /* The greater-than operator should expand to the named partial. */
        [TestMethod]
        public void TestPartialsBasicBehavior()
        {
            string template = "\"{{>text}}\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { { "text", "from partial" } };
            object data = new { };
            string expected = "\"from partial\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* The empty string should be used when the named partial is not found. */
        [TestMethod]
        public void TestPartialsFailedLookup()
        {
            string template = "\"{{>text}}\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { };
            string expected = "\"\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* The greater-than operator should operate within the current context. */
        [TestMethod]
        public void TestPartialsContext()
        {
            string template = "\"{{>partial}}\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { { "partial", "*{{text}}*" } };
            object data = new { text = "content" };
            string expected = "\"*content*\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* The greater-than operator should properly recurse. */
        [TestMethod]
        public void TestPartialsRecursion()
        {
            string template = "{{>node}}";
            Dictionary<string, string> partials = new Dictionary<string, string> { { "node", "{{content}}<{{#nodes}}{{>node}}{{/nodes}}>" } };
            object data = new { content = "X", nodes = new object[] { new { content = "Y", nodes = new object[] { } } } };
            string expected = "X<Y<>>";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* The greater-than operator should not alter surrounding whitespace. */
        [TestMethod]
        public void TestPartialsSurroundingWhitespace()
        {
            string template = "| {{>partial}} |";
            Dictionary<string, string> partials = new Dictionary<string, string> { { "partial", "	|	" } };
            object data = new { };
            string expected = "| 	|	 |";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Whitespace should be left untouched. */
        [TestMethod]
        public void TestPartialsInlineIndentation()
        {
            string template = "  {{data}}  {{> partial}}\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { { "partial", ">\n>" } };
            object data = new { data = "|" };
            string expected = "  |  >\n>\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* "\r\n" should be considered a newline for standalone tags. */
        [TestMethod]
        public void TestPartialsStandaloneLineEndings()
        {
            string template = "|\r\n{{>partial}}\r\n|";
            Dictionary<string, string> partials = new Dictionary<string, string> { { "partial", ">" } };
            object data = new { };
            string expected = "|\r\n>|";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Standalone tags should not require a newline to precede them. */
        [TestMethod]
        public void TestPartialsStandaloneWithoutPreviousLine()
        {
            string template = "  {{>partial}}\n>";
            Dictionary<string, string> partials = new Dictionary<string, string> { { "partial", ">\n>" } };
            object data = new { };
            string expected = "  >\n  >>";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Standalone tags should not require a newline to follow them. */
        [TestMethod]
        public void TestPartialsStandaloneWithoutNewline()
        {
            string template = ">\n  {{>partial}}";
            Dictionary<string, string> partials = new Dictionary<string, string> { { "partial", ">\n>" } };
            object data = new { };
            string expected = ">\n  >\n  >";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Each line of the partial should be indented before rendering. */
        [TestMethod]
        public void TestPartialsStandaloneIndentation()
        {
            string template = "\\n {{>partial}}\n/\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { { "partial", "|\n{{{content}}}\n|\n" } };
            object data = new { content = "<\n->" };
            string expected = "\\n |\n <\n->\n |\n/\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Superfluous in-tag whitespace should be ignored. */
        [TestMethod]
        public void TestPartialsPaddingWhitespace()
        {
            string template = "|{{> partial }}|";
            Dictionary<string, string> partials = new Dictionary<string, string> { { "partial", "[]" } };
            object data = new { boolean = true };
            string expected = "|[]|";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        #endregion
        /*
Section tags and End Section tags are used in combination to wrap a section
of the template for iteration

These tags' content MUST be a non-whitespace character sequence NOT
containing the current closing delimiter; each Section tag MUST be followed
by an End Section tag with the same content within the same section.

This tag's content names the data to replace the tag.  Name resolution is as
follows:
  1) Split the name on periods; the first part is the name to resolve, any
  remaining parts should be retained.
  2) Walk the context stack from top to bottom, finding the first context
  that is a) a hash containing the name as a key OR b) an object responding
  to a method with the given name.
  3) If the context is a hash, the data is the value associated with the
  name.
  4) If the context is an object and the method with the given name has an
  arity of 1, the method SHOULD be called with a String containing the
  unprocessed contents of the sections; the data is the value returned.
  5) Otherwise, the data is the value returned by calling the method with
  the given name.
  6) If any name parts were retained in step 1, each should be resolved
  against a context stack containing only the result from the former
  resolution.  If any part fails resolution, the result should be considered
  falsey, and should interpolate as the empty string.
If the data is not of a list type, it is coerced into a list as follows: if
the data is truthy (e.g. `!!data == true`), use a single-element list
containing the data, otherwise use an empty list.

For each element in the data list, the element MUST be pushed onto the
context stack, the section MUST be rendered, and the element MUST be popped
off the context stack.

Section and End Section tags SHOULD be treated as standalone when
appropriate.

*/
        #region Sections
        /* Truthy sections should have their contents rendered. */
        [TestMethod]
        public void TestSectionsTruthy()
        {
            string template = "\"{{#boolean}}This should be rendered.{{/boolean}}\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { boolean = true };
            string expected = "\"This should be rendered.\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Falsey sections should have their contents omitted. */
        [TestMethod]
        public void TestSectionsFalsey()
        {
            string template = "\"{{#boolean}}This should not be rendered.{{/boolean}}\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { boolean = false };
            string expected = "\"\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Objects and hashes should be pushed onto the context stack. */
        [TestMethod]
        public void TestSectionsContext()
        {
            string template = "\"{{#context}}Hi {{name}}.{{/context}}\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { context = new { name = "Joe" } };
            string expected = "\"Hi Joe.\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* All elements on the context stack should be accessible. */
        [TestMethod]
        public void TestSectionsDeeplyNestedContexts()
        {
            string template = "{{#a}}\n{{one}}\n{{#b}}\n{{one}}{{two}}{{one}}\n{{#c}}\n{{one}}{{two}}{{three}}{{two}}{{one}}\n{{#d}}\n{{one}}{{two}}{{three}}{{four}}{{three}}{{two}}{{one}}\n{{#e}}\n{{one}}{{two}}{{three}}{{four}}{{five}}{{four}}{{three}}{{two}}{{one}}\n{{/e}}\n{{one}}{{two}}{{three}}{{four}}{{three}}{{two}}{{one}}\n{{/d}}\n{{one}}{{two}}{{three}}{{two}}{{one}}\n{{/c}}\n{{one}}{{two}}{{one}}\n{{/b}}\n{{one}}\n{{/a}}\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { a = new { one = 1 }, b = new { two = 2 }, c = new { three = 3 }, d = new { four = 4 }, e = new { five = 5 } };
            string expected = "1\n121\n12321\n1234321\n123454321\n1234321\n12321\n121\n1\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Lists should be iterated; list items should visit the context stack. */
        [TestMethod]
        public void TestSectionsList()
        {
            string template = "\"{{#list}}{{item}}{{/list}}\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { list = new object[] { new { item = 1 }, new { item = 2 }, new { item = 3 } } };
            string expected = "\"123\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Empty lists should behave like falsey values. */
        [TestMethod]
        public void TestSectionsEmptyList()
        {
            string template = "\"{{#list}}Yay lists!{{/list}}\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { list = new object[] { } };
            string expected = "\"\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Multiple sections per template should be permitted. */
        [TestMethod]
        public void TestSectionsDoubled()
        {
            string template = "{{#bool}}\n* first\n{{/bool}}\n* {{two}}\n{{#bool}}\n* third\n{{/bool}}\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { two = "second", @bool = true };
            string expected = "* first\n* second\n* third\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Nested truthy sections should have their contents rendered. */
        [TestMethod]
        public void TestSectionsNestedTruthy()
        {
            string template = "| A {{#bool}}B {{#bool}}C{{/bool}} D{{/bool}} E |";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { @bool = true };
            string expected = "| A B C D E |";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Nested falsey sections should be omitted. */
        [TestMethod]
        public void TestSectionsNestedFalsey()
        {
            string template = "| A {{#bool}}B {{#bool}}C{{/bool}} D{{/bool}} E |";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { @bool = false };
            string expected = "| A  E |";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Failed context lookups should be considered falsey. */
        [TestMethod]
        public void TestSectionsContextMisses()
        {
            string template = "[{{#missing}}Found key 'missing'!{{/missing}}]";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { };
            string expected = "[]";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Implicit iterators should directly interpolate strings. */
        [TestMethod]
        public void TestSectionsImplicitIteratorString()
        {
            string template = "\"{{#list}}({{.}}){{/list}}\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { list = new object[] { "a", "b", "c", "d", "e" } };
            string expected = "\"(a)(b)(c)(d)(e)\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Implicit iterators should cast integers to strings and interpolate. */
        [TestMethod]
        public void TestSectionsImplicitIteratorInteger()
        {
            string template = "\"{{#list}}({{.}}){{/list}}\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { list = new object[] { 1, 2, 3, 4, 5 } };
            string expected = "\"(1)(2)(3)(4)(5)\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Implicit iterators should cast decimals to strings and interpolate. */
        [TestMethod]
        public void TestSectionsImplicitIteratorDecimal()
        {
            string template = "\"{{#list}}({{.}}){{/list}}\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { list = new object[] { 1.1, 2.2, 3.3, 4.4, 5.5 } };
            string expected = "\"(1.1)(2.2)(3.3)(4.4)(5.5)\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Dotted names should be valid for Section tags. */
        [TestMethod]
        public void TestSectionsDottedNamesTruthy()
        {
            string template = "\"{{#a.b.c}}Here{{/a.b.c}}\" == \"Here\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { a = new { b = new { c = true } } };
            string expected = "\"Here\" == \"Here\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Dotted names should be valid for Section tags. */
        [TestMethod]
        public void TestSectionsDottedNamesFalsey()
        {
            string template = "\"{{#a.b.c}}Here{{/a.b.c}}\" == \"\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { a = new { b = new { c = false } } };
            string expected = "\"\" == \"\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Dotted names that cannot be resolved should be considered falsey. */
        [TestMethod]
        public void TestSectionsDottedNamesBrokenChains()
        {
            string template = "\"{{#a.b.c}}Here{{/a.b.c}}\" == \"\"";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { a = new { } };
            string expected = "\"\" == \"\"";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Sections should not alter surrounding whitespace. */
        [TestMethod]
        public void TestSectionsSurroundingWhitespace()
        {
            string template = " | {{#boolean}}	|	{{/boolean}} | \n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { boolean = true };
            string expected = " | 	|	 | \n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Sections should not alter internal whitespace. */
        [TestMethod]
        public void TestSectionsInternalWhitespace()
        {
            string template = " | {{#boolean}} {{! Important Whitespace }}\n {{/boolean}} | \n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { boolean = true };
            string expected = " |  \n  | \n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Single-line sections should not alter surrounding whitespace. */
        [TestMethod]
        public void TestSectionsIndentedInlineSections()
        {
            string template = " {{#boolean}}YES{{/boolean}}\n {{#boolean}}GOOD{{/boolean}}\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { boolean = true };
            string expected = " YES\n GOOD\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Standalone lines should be removed from the template. */
        [TestMethod]
        public void TestSectionsStandaloneLines()
        {
            string template = "| This Is\n{{#boolean}}\n|\n{{/boolean}}\n| A Line\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { boolean = true };
            string expected = "| This Is\n|\n| A Line\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Indented standalone lines should be removed from the template. */
        [TestMethod]
        public void TestSectionsIndentedStandaloneLines()
        {
            string template = "| This Is\n  {{#boolean}}\n|\n  {{/boolean}}\n| A Line\n";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { boolean = true };
            string expected = "| This Is\n|\n| A Line\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* "\r\n" should be considered a newline for standalone tags. */
        [TestMethod]
        public void TestSectionsStandaloneLineEndings()
        {
            string template = "|\r\n{{#boolean}}\r\n{{/boolean}}\r\n|";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { boolean = true };
            string expected = "|\r\n|";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Standalone tags should not require a newline to precede them. */
        [TestMethod]
        public void TestSectionsStandaloneWithoutPreviousLine()
        {
            string template = "  {{#boolean}}\n#{{/boolean}}\n/";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { boolean = true };
            string expected = "#\n/";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Standalone tags should not require a newline to follow them. */
        [TestMethod]
        public void TestSectionsStandaloneWithoutNewline()
        {
            string template = "#{{#boolean}}\n/\n  {{/boolean}}";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { boolean = true };
            string expected = "#\n/\n";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        /* Superfluous in-tag whitespace should be ignored. */
        [TestMethod]
        public void TestSectionsPadding()
        {
            string template = "|{{# boolean }}={{/ boolean }}|";
            Dictionary<string, string> partials = new Dictionary<string, string> { };
            object data = new { boolean = true };
            string expected = "|=|";
            Assert.AreEqual(expected, SharpStache.Render(template, partials, data));
        }
        #endregion

    }
}
