using Shap.Flexer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;


namespace Shap.ShapDoc.Parser
{
    using SDTokenType = ShapDocTokenizer.TokenType;
    using SDToken = StringFlexer<ShapDocTokenizer.TokenType>.Token;
    using CharClass = StringFlexer<ShapDocTokenizer.TokenType>.CharClass;
    using TokenizerFlexer = StringFlexer<ShapDocTokenizer.TokenType>;

    public class ShapDocDeserializer
    {
        private readonly ShapDocParser parser = new();
        private readonly ShapDocTokenizer tokenizer = new();

        public ShapDoc Deserialize(IEnumerable<char> input)
        {
            return parser.Parse(tokenizer.Tokenize(input));
        }
    }

    internal class ShapDocParser
    {
        public enum ParserState
        {
            Free,
            InTag
        }

        public class ProcessorCtx
        {
            public ShapDoc.Node curNode = new("__root", null);
            public List<string> curWords = [];
            public string newNodeName = "", newNodeParamName = "";
            public bool newNodeHasClosingModifier = false, newNodeHasSingletonModifier = false;
            public Dictionary<string, string?> newNodeParams = [];
        }

        readonly CallbackFlexer<ParserState, SDToken, TokenTypeComparer<SDTokenType>, ProcessorCtx> flexer;

        public ShapDocParser()
        {
            flexer = new();

            flexer.Register(ParserState.Free)
                .When(new(SDTokenType.Word)).Do((ParserState curState, IEnumerator<SDToken> input, ProcessorCtx ctx) =>
                {
                    SDToken curToken = input.Current;
                    ctx.curWords.Add(curToken.str);
                    return curState;
                }).Next()
                .When(new(SDTokenType.Space)).Do((ParserState curState, IEnumerator<SDToken> input, ProcessorCtx ctx) =>
                {
                    SDToken curToken = input.Current;
                    ctx.curWords.Add(" ");
                    return curState;
                }).Next()
                .When(new(SDTokenType.ParagraphBreak)).Do((ParserState curState, IEnumerator<SDToken> input, ProcessorCtx ctx) =>
                {
                    SDToken curToken = input.Current;
                    ctx.curNode.Children.Add(new("t", ctx.curNode, new() { { "text", string.Join("", ctx.curWords) } }));
                    ctx.curWords = [];
                    ctx.curNode.Children.Add(new("br", ctx.curNode));
                    return curState;
                }).Next()
                .When(new(SDTokenType.PreformattedText)).Do((ParserState curState, IEnumerator<SDToken> input, ProcessorCtx ctx) =>
                {
                    SDToken curToken = input.Current;
                    ctx.curWords.Add(curToken.str);
                    return curState;
                }).Next()
                .When(new(SDTokenType.EscapeSeq)).Do((ParserState curState, IEnumerator<SDToken> input, ProcessorCtx ctx) =>
                {
                    SDToken curToken = input.Current;
                    ctx.curWords.Add(curToken.str switch
                    {
                        "quot" => "\"",
                        "apos" => "'",
                        "amp" => "&",
                        "lt" => "<",
                        "rt" => ">",
                        _ => throw SyntaxException.FromToken(curToken, "Invalid escape sequence")
                    });
                    return curState;
                }).Next()
                .When(new(SDTokenType.TagBegin)).Do((ParserState curState, IEnumerator<SDToken> input, ProcessorCtx ctx) =>
                {
                    if (ctx.curWords.Count != 0)
                    {
                        ctx.curNode.Children.Add(new("t", ctx.curNode, new() { { "text", string.Join("", ctx.curWords) } }));
                        ctx.curWords = [];
                    }

                    return ParserState.InTag;
                }).Next()
                .Otherwise().Error("Tag tokens outside of tag")
                .WhenEndOk();

            flexer.Register(ParserState.InTag)
                .When(new(SDTokenType.TagWord)).Do((ParserState curState, IEnumerator<SDToken> input, ProcessorCtx ctx) =>
                {
                    SDToken curToken = input.Current;
                    if (ctx.newNodeName == "") ctx.newNodeName = curToken.str;
                    else
                    {
                        if (ctx.newNodeParamName != "")
                        {
                            if (ctx.newNodeParams.ContainsKey(ctx.newNodeParamName))
                                throw SyntaxException.FromToken(curToken, "Specified param already present");
                            ctx.newNodeParams.Add(ctx.newNodeParamName, null);
                        }
                        ctx.newNodeParamName = curToken.str;
                    }
                    return curState;
                }).Next()
                .When(new(SDTokenType.TagModifierClosingTag)).Do((ParserState curState, IEnumerator<SDToken> input, ProcessorCtx ctx) =>
                {
                    SDToken curToken = input.Current;
                    if (ctx.newNodeHasClosingModifier || ctx.newNodeHasSingletonModifier)
                        throw SyntaxException.FromToken(curToken, "Specified tag already has modifier");
                    ctx.newNodeHasClosingModifier = true;
                    return curState;
                }).Next()
                .When(new(SDTokenType.TagModifierSingletonTag)).Do((ParserState curState, IEnumerator<SDToken> input, ProcessorCtx ctx) =>
                {
                    SDToken curToken = input.Current;
                    if (ctx.newNodeHasClosingModifier || ctx.newNodeHasSingletonModifier)
                        throw SyntaxException.FromToken(curToken, "Specified tag already has modifier");
                    ctx.newNodeHasSingletonModifier = true;
                    return curState;
                }).Next()
                .When(new(SDTokenType.TagParamValue)).Do((ParserState curState, IEnumerator<SDToken> input, ProcessorCtx ctx) =>
                {
                    SDToken curToken = input.Current;
                    if (ctx.newNodeParamName == "")
                        throw SyntaxException.FromToken(curToken, "Param value encountered with no param name");
                    if (ctx.newNodeParams.ContainsKey(ctx.newNodeParamName))
                        throw SyntaxException.FromToken(curToken, "Specified param already present");

                    ctx.newNodeParams.Add(ctx.newNodeParamName, curToken.str);
                    ctx.newNodeParamName = "";
                    return curState;
                }).Next()
                .When(new(SDTokenType.TagEnd)).Do((ParserState curState, IEnumerator<SDToken> input, ProcessorCtx ctx) =>
                {
                    SDToken curToken = input.Current;
                    if (ctx.newNodeName == "")
                        throw SyntaxException.FromToken(curToken, "Missing tag name");

                    if (ctx.newNodeHasClosingModifier)
                    {
                        if (ctx.newNodeParams.Count != 0)
                            throw SyntaxException.FromToken(curToken, "Closing tags aren't allowed to have params");
                        if (ctx.curNode.Name != ctx.newNodeName)
                            throw SyntaxException.FromToken(curToken, "Closing and opening tags mismatch");
                        if (ctx.curNode.Parent == null)
                            throw SyntaxException.FromToken(curToken, "No opening node for this closing node");
                        ctx.curNode = ctx.curNode.Parent;
                    }
                    else
                    {
                        ShapDoc.Node newNode = new(ctx.newNodeName, ctx.curNode, ctx.newNodeParams);

                        if (ctx.newNodeHasSingletonModifier)
                        {
                            ctx.curNode.Children.Add(newNode);
                        }
                        else
                        {
                            //opening node
                            ctx.curNode.Children.Add(newNode);
                            ctx.curNode = newNode;
                        }
                    }

                    ctx.newNodeName = "";
                    ctx.newNodeParamName = "";
                    ctx.newNodeHasClosingModifier = false;
                    ctx.newNodeHasSingletonModifier = false;
                    ctx.newNodeParams = [];
                    ctx.curWords = [];
                    return ParserState.Free;
                }).Next()
                .Otherwise().Error("Non-tag tokens inside tag")
                .WhenEndError("Unexpected EOF");
        }

        public ShapDoc Parse(IEnumerable<SDToken> input)
        {
            ProcessorCtx ctx = new();
            flexer.FlexAll(input, ctx);
            if (ctx.curNode.Name != "__root")
            {
                throw SyntaxException.FromToken(input.Last(), "Last tag is unclosed");
            }
            return new(ctx.curNode);
        }

        public ShapDoc Parse(IEnumerable<char> input)
        {
            return Parse(new ShapDocTokenizer().Tokenize(input));
        }
    }

    internal class ShapDocTokenizer
    {
        public enum TokenType
        {
            Free,
            Word,
            Space,
            ParagraphBreakOrSpace,
            ParagraphBreak,

            PreformattedText,

            EscapeSeq,

            TagBegin,
            TagWord,
            TagModifierClosingTag,
            TagModifierSingletonTag,
            TagSpace,
            TagEquals,
            TagParamValue,
            TagEnd,
        }

        readonly TokenizerFlexer flexer;

        public ShapDocTokenizer()
        {
            flexer = new();
            flexer.Register(TokenType.Free)
                .When(CharClass.Whitespace).ChangeState(TokenType.Space)
                .When('\n').ChangeState(TokenType.ParagraphBreakOrSpace).Skip()
                .When('"').ChangeState(TokenType.PreformattedText).Skip()
                .When('&').ChangeState(TokenType.EscapeSeq).Skip()
                .When('<').ChangeState(TokenType.TagBegin).Skip()
                .When('>').Error("Closing tag outside of tag structure")
                .Otherwise().ChangeState(TokenType.Word)
                .WhenEofDropState();
            flexer.Register(TokenType.Word)
                .When(CharClass.Whitespace).NextState(TokenType.Space)
                .When('\n').NextState(TokenType.ParagraphBreakOrSpace).Skip()
                .When('"').NextState(TokenType.PreformattedText).Skip()
                .When('&').NextState(TokenType.EscapeSeq).Skip()
                .When('<').NextState(TokenType.TagBegin).Skip()
                .When('>').Error("Closing tag outside of tag structure")
                .Otherwise().Consume()
                .WhenEofYieldState();
            flexer.Register(TokenType.Space)
                .When(CharClass.Whitespace).Skip()
                .When('\n').ChangeState(TokenType.ParagraphBreakOrSpace).Skip()
                .When('"').NextState(TokenType.PreformattedText).Skip()
                .When('&').NextState(TokenType.EscapeSeq).Skip()
                .When('<').NextState(TokenType.TagBegin).Skip()
                .When('>').Error("Closing tag outside of tag structure")
                .Otherwise().NextState(TokenType.Word)
                .WhenEofDropState();
            flexer.Register(TokenType.ParagraphBreakOrSpace)
                .When(CharClass.Whitespace).Skip()
                .When('\n').ChangeState(TokenType.ParagraphBreak).Skip()
                .When('"').ChangeState(TokenType.Space).NextState(TokenType.PreformattedText).Skip()
                .When('&').ChangeState(TokenType.Space).NextState(TokenType.EscapeSeq).Skip()
                .When('<').ChangeState(TokenType.Space).NextState(TokenType.TagBegin).Skip()
                .When('>').Error("Closing tag outside of tag structure")
                .Otherwise().ChangeState(TokenType.Space).NextState(TokenType.Word)
                .WhenEofDropState();
            flexer.Register(TokenType.ParagraphBreak)
                .When(CharClass.Whitespace, '\n').Skip()
                .When('"').NextState(TokenType.PreformattedText).Skip()
                .When('&').NextState(TokenType.EscapeSeq).Skip()
                .When('<').NextState(TokenType.TagBegin).Skip()
                .When('>').Error("Closing tag outside of tag structure")
                .Otherwise().NextState(TokenType.Word)
                .WhenEofDropState();

            flexer.Register(TokenType.PreformattedText)
                .When('"').NextState(TokenType.Free).Skip()
                .Otherwise().Consume()
                .WhenEofError();

            flexer.Register(TokenType.EscapeSeq)
                .When(';').NextState(TokenType.Free).Skip()
                .When(CharClass.Number, CharClass.LatinLetter).Consume()
                .Otherwise().Error("Invalid character in escape sequence")
                .WhenEofError();

            flexer.Register(TokenType.TagBegin)
                .When('/').NextState(TokenType.TagModifierClosingTag).Skip()
                .When(CharClass.LatinLetter).NextState(TokenType.TagWord)
                .Otherwise().Error("Invalid character in tag")
                .WhenEofError();
            flexer.Register(TokenType.TagWord)
                .When(CharClass.LatinLetter, CharClass.Number, '_', '-').Consume()
                .When(CharClass.Whitespace, '\n').NextState(TokenType.TagSpace)
                .When('=').NextState(TokenType.TagEquals).Skip()
                .When('/').NextState(TokenType.TagModifierSingletonTag).Skip()
                .When('>').NextState(TokenType.TagEnd).Skip()
                .Otherwise().Error("Invalid character in tag")
                .WhenEofError();

            flexer.Register(TokenType.TagModifierClosingTag)
                .When(CharClass.LatinLetter).NextState(TokenType.TagWord).Consume()
                .Otherwise().Error("Invalid character in tag")
                .WhenEofError();

            flexer.Register(TokenType.TagModifierSingletonTag)
                .When('>').NextState(TokenType.TagEnd).Skip()
                .Otherwise().Error("Invalid character in tag")
                .WhenEofError();

            flexer.Register(TokenType.TagSpace)
                .When(CharClass.Whitespace, '\n').Skip()
                .When(CharClass.LatinLetter).ChangeState(TokenType.TagWord).Consume()
                .When('=').ChangeState(TokenType.TagEquals).Skip()
                .Otherwise().Error("Invalid character in tag")
                .WhenEofError();

            flexer.Register(TokenType.TagEquals)
                .When(CharClass.Whitespace).Skip()
                .When('"').ChangeState(TokenType.TagParamValue).Skip()
                .Otherwise().Error("Invalid character in tag")
                .WhenEofError();

            flexer.Register(TokenType.TagParamValue)
                .When('"').Skip().NextState(TokenType.TagWord)
                .Otherwise().Consume()
                .WhenEofError();
            flexer.Register(TokenType.TagEnd)
                .Otherwise().NextState(TokenType.Free)
                .WhenEofYieldState();
        }

        public IEnumerable<TokenizerFlexer.Token> Tokenize(IEnumerable<char> input) => flexer.Flex(input);
    }
}
