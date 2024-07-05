using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shap.Flexer;

namespace Shap.ShapDoc.CodeConverter
{
    using FTTokenType = FTTokenizer.TokenType;
    using FTToken = StringFlexer<FTTokenizer.TokenType>.Token;
    using CharClass = StringFlexer<FTTokenizer.TokenType>.CharClass;
    using TokenizerFlexer = StringFlexer<FTTokenizer.TokenType>;

    public class FormattedTextDeserializer
    {
        readonly FTParser parser = new();

        public List<List<ColoredSpan>> Deserialize(IEnumerable<char> input)
        {
            return parser.Parse(input);
        }
    }

    public readonly struct ColoredSpan(string str, string colorCode)
    {
        public readonly string str = str, colorCode = colorCode;
    }

    internal class FTParser
    {
        enum ParserState
        {
            SkipUntilFirstDiv,
            Free,
            InsideSpan,
            SkipUntilEnd
        }

        class ProcessorCtx
        {
            public List<List<ColoredSpan>> code = [];
            public List<ColoredSpan> line = [];
            public List<string> curSpanStr = [];
            public string cutSpanColorStr = "";

            public ProcessorCtx()
            {
                code.Add(line);
            }
        }

        readonly CallbackFlexer<ParserState, FTToken, TokenTypeComparer<FTTokenType>, ProcessorCtx> flexer = new();

        public FTParser()
        {
            flexer.Register(ParserState.SkipUntilFirstDiv)
                .When(new(FTTokenType.Tag)).Do((ParserState curState, IEnumerator<FTToken> input, ProcessorCtx ctx) =>
                {
                    if (input.Current.str == "div") return ParserState.Free;
                    else return curState;
                }).Next()
                .When(new(FTTokenType.Text)).Next()
                .Otherwise().Error()
                .WhenEndOk();

            flexer.Register(ParserState.Free)
                .When(new(FTTokenType.Tag)).Do((ParserState curState, IEnumerator<FTToken> input, ProcessorCtx ctx) =>
                {
                    FTToken curToken = input.Current;
                    if (curToken.str[0] == '!') return curState;    //comment tag, skip

                    string tagName = curToken.str.Split(' ')[0];
                    switch (tagName)
                    {
                        case "/div":
                        case "br":
                            ctx.line = [];
                            ctx.code.Add(ctx.line);
                            return ParserState.Free;
                        case "/body":
                            return ParserState.SkipUntilEnd;
                        case "div":
                            return ParserState.Free;
                        case "span":
                            ctx.curSpanStr = [];
                            ctx.cutSpanColorStr = curToken.str[20..26];
                            return ParserState.InsideSpan;
                        default:
                            throw SyntaxException.FromToken(curToken, "Bad tag");
                    }
                }).Next()
                .When(new(FTTokenType.Text)).Next()
                .Otherwise().Error()
                .WhenEndError();
            
            flexer.Register(ParserState.InsideSpan)
                .When(new(FTTokenType.Text)).Do((ParserState curState, IEnumerator<FTToken> input, ProcessorCtx ctx) =>
                {
                    ctx.curSpanStr.Add(input.Current.str);
                    return ParserState.InsideSpan;
                }).Next()
                //.When(new(FTTokenType.EscapeSeq)).Do((ParserState curState, IEnumerator<FTToken> input, ProcessorCtx ctx) =>
                //{
                //    FTToken curToken = input.Current;
                //    if (curToken.str[0] == '#')
                //    {
                //        int unicode = int.Parse(curToken.str[1..]);
                //        ctx.curSpanStr.Add(((char)unicode).ToString());
                //    }
                //    else
                //    {
                //        ctx.curSpanStr.Add(curToken.str switch
                //        {
                //            "quot" => "\"",
                //            "apos" => "'",
                //            "amp" => "&",
                //            "lt" => "<",
                //            "rt" => ">",
                //            _ => throw SyntaxException.FromToken(curToken, "Invalid escape sequence")
                //        });
                //    }
                //    return ParserState.InsideSpan;
                //}).Next()
                .When(new(FTTokenType.Tag)).Do((ParserState curState, IEnumerator<FTToken> input, ProcessorCtx ctx) =>
                {
                    FTToken curToken = input.Current;
                    if (curToken.str == "/span")
                    {
                        ctx.line.Add(new(string.Join("", ctx.curSpanStr), ctx.cutSpanColorStr));
                        return ParserState.Free;
                    }
                    else
                    {
                        throw SyntaxException.FromToken(curToken, "Bad closing tag");
                    }
                }).Next()
                .Otherwise().Error()
                .WhenEndError();

            flexer.Register(ParserState.SkipUntilEnd)
                .Otherwise().Next()
                .WhenEndOk();
        }

        public List<List<ColoredSpan>> Parse(IEnumerable<FTToken> input)
        {
            ProcessorCtx ctx = new();
            flexer.FlexAll(input, ctx);
            return ctx.code;
        }

        public List<List<ColoredSpan>> Parse(IEnumerable<char> input)
        {
            return Parse(new FTTokenizer().Tokenize(input));
        }
    }

    internal class FTTokenizer
    {
        public enum TokenType
        {
            Free,
            Text,       //token
            //EscapeSeq,  //token
            Tag,        //token
        }

        readonly TokenizerFlexer flexer = new();

        public FTTokenizer()
        {
            flexer.Register(FTTokenType.Free)
                .When('<').ChangeState(FTTokenType.Tag).Skip()
                .When('>').Error("Closing token fivebidden outside of tag")
                //.When('&').ChangeState(FTTokenType.EscapeSeq).Skip()
                .Otherwise().ChangeState(FTTokenType.Text)
                .WhenEofDropState();

            flexer.Register(FTTokenType.Text)
                .When('<').NextState(FTTokenType.Tag).Skip()
                .When('>').Error("Closing token fivebidden outside of tag")
                //.When('&').NextState(FTTokenType.EscapeSeq).Skip()
                .Otherwise().Consume()
                .WhenEofYieldState();

            //flexer.Register(FTTokenType.EscapeSeq)
            //    .When(CharClass.LatinLetter, CharClass.Number, '#').Consume()
            //    .When(';').NextState(FTTokenType.Free).Skip()
            //    .Otherwise().Error("Invalid escape sequence")
            //    .WhenEofError();

            flexer.Register(FTTokenType.Tag)
                .When('>').NextState(FTTokenType.Free).Skip()
                .When('<').Error("Nested tags forbidden")
                .Otherwise().Consume()
                .WhenEofError();
        }

        public List<FTToken> Tokenize(IEnumerable<char> input) => flexer.FlexAll(input);
    }

    //internal class HtmlParser
    //{
    //    public enum ParserState
    //    {
    //        Free,
    //        InTag
    //    }

    //    public class ProcessorCtx
    //    {
    //        public List<List<ColoredSpan>> lines = [];
    //        public List<ColoredSpan> curLine = [];
    //        public List<string> curText = [];
    //        public string newNodeName = "", newNodeParamName = "";
    //        public bool newNodeHasClosingModifier = false, newNodeHasSingletonModifier = false;
    //        public Dictionary<string, string?> newNodeParams = [];
    //    }

    //    readonly CallbackFlexer<ParserState, XmlToken, TokenTypeComparer<FTTokenType>, ProcessorCtx> flexer;

    //    public HtmlParser()
    //    {
    //        flexer = new();

    //        flexer.Register(ParserState.Free)
    //            .When(new(FTTokenType.Text)).Do((ParserState curState, IEnumerator<XmlToken> input, ProcessorCtx ctx) =>
    //            {
    //                XmlToken curToken = input.Current;
    //                ctx.curText.Add(curToken.str);
    //                return curState;
    //            }).Next()
    //            .When(new(FTTokenType.Space)).Do((ParserState curState, IEnumerator<XmlToken> input, ProcessorCtx ctx) =>
    //            {
    //                XmlToken curToken = input.Current;
    //                ctx.curText.Add(" ");
    //                return curState;
    //            }).Next()
    //            .When(new(FTTokenType.EscapeSeq)).Do((ParserState curState, IEnumerator<XmlToken> input, ProcessorCtx ctx) =>
    //            {
    //                XmlToken curToken = input.Current;
    //                if (curToken.str[0] == '#')
    //                {
    //                    int unicode = int.Parse(curToken.str[1..]);
    //                    ctx.curText.Add(((char)unicode).ToString());
    //                }
    //                else
    //                {
    //                    ctx.curText.Add(curToken.str switch
    //                    {
    //                        "quot" => "\"",
    //                        "apos" => "'",
    //                        "amp" => "&",
    //                        "lt" => "<",
    //                        "rt" => ">",
    //                        _ => throw SyntaxException.FromToken(curToken, "Invalid escape sequence")
    //                    });
    //                }
                    
    //                return curState;
    //            }).Next()
    //            .When(new(FTTokenType.TagBegin)).Do((ParserState curState, IEnumerator<XmlToken> input, ProcessorCtx ctx) =>
    //            {
    //                return ParserState.InTag;
    //            }).Next()
    //            .Otherwise().Error("Tag tokens outside of tag")
    //            .WhenEndOk();

    //        flexer.Register(ParserState.InTag)
    //            .When(new(FTTokenType.TagWord)).Do((ParserState curState, IEnumerator<XmlToken> input, ProcessorCtx ctx) =>
    //            {
    //                XmlToken curToken = input.Current;
    //                if (ctx.newNodeName == "") ctx.newNodeName = curToken.str;
    //                else
    //                {
    //                    if (ctx.newNodeParamName != "")
    //                    {
    //                        if (ctx.newNodeParams.ContainsKey(ctx.newNodeParamName))
    //                            throw SyntaxException.FromToken(curToken, "Specified param already present");
    //                        ctx.newNodeParams.Add(ctx.newNodeParamName, null);
    //                    }
    //                    ctx.newNodeParamName = curToken.str;
    //                }
    //                return curState;
    //            }).Next()
    //            .When(new(FTTokenType.TagModifierClosingTag)).Do((ParserState curState, IEnumerator<XmlToken> input, ProcessorCtx ctx) =>
    //            {
    //                XmlToken curToken = input.Current;
    //                if (ctx.newNodeHasClosingModifier || ctx.newNodeHasSingletonModifier)
    //                    throw SyntaxException.FromToken(curToken, "Specified tag already has modifier");
    //                ctx.newNodeHasClosingModifier = true;
    //                return curState;
    //            }).Next()
    //            .When(new(FTTokenType.TagModifierSingletonTag)).Do((ParserState curState, IEnumerator<XmlToken> input, ProcessorCtx ctx) =>
    //            {
    //                XmlToken curToken = input.Current;
    //                if (ctx.newNodeHasClosingModifier || ctx.newNodeHasSingletonModifier)
    //                    throw SyntaxException.FromToken(curToken, "Specified tag already has modifier");
    //                ctx.newNodeHasSingletonModifier = true;
    //                return curState;
    //            }).Next()
    //            .When(new(FTTokenType.TagParamValue)).Do((ParserState curState, IEnumerator<XmlToken> input, ProcessorCtx ctx) =>
    //            {
    //                XmlToken curToken = input.Current;
    //                if (ctx.newNodeParamName == "")
    //                    throw SyntaxException.FromToken(curToken, "Param value encountered with no param name");
    //                if (ctx.newNodeParams.ContainsKey(ctx.newNodeParamName))
    //                    throw SyntaxException.FromToken(curToken, "Specified param already present");

    //                ctx.newNodeParams.Add(ctx.newNodeParamName, curToken.str);
    //                ctx.newNodeParamName = "";
    //                return curState;
    //            }).Next()
    //            .When(new(FTTokenType.TagEnd)).Do((ParserState curState, IEnumerator<XmlToken> input, ProcessorCtx ctx) =>
    //            {
    //                XmlToken curToken = input.Current;
    //                if (ctx.newNodeName == "")
    //                    throw SyntaxException.FromToken(curToken, "Missing tag name");

    //                if (ctx.newNodeHasClosingModifier)
    //                {

    //                    if (ctx.newNodeParams.Count != 0)
    //                    {
    //                        throw SyntaxException.FromToken(curToken, "Closing tags aren't allowed to have params");
    //                    }

    //                    if (this.forgiveUnclosedTags)
    //                    {
    //                        do
    //                        {
    //                            if (ctx.curNode.ParentNode == null) break;
    //                            ctx.curNode = (XmlElement)ctx.curNode.ParentNode;
    //                        } while (ctx.curNode.Name != ctx.newNodeName);
    //                    }
    //                    else
    //                    {
    //                        if (ctx.curNode.Name != ctx.newNodeName)
    //                            throw SyntaxException.FromToken(curToken, "Closing and opening tags mismatch");
    //                        if (ctx.curNode.ParentNode == null)
    //                            throw SyntaxException.FromToken(curToken, "No opening node for this closing node");
    //                        ctx.curNode = (XmlElement)ctx.curNode.ParentNode;
    //                    }
    //                }
    //                else
    //                {
    //                    XmlElement newNode = ctx.doc.CreateElement(ctx.newNodeName);
    //                    foreach ((string paramName, string? paramValue) in ctx.newNodeParams)
    //                    {
    //                        newNode.SetAttribute(paramName, paramValue);
    //                    }

    //                    if (ctx.newNodeHasSingletonModifier)
    //                    {
    //                        ctx.curNode.AppendChild(newNode);
    //                    }
    //                    else
    //                    {
    //                        //opening node
    //                        ctx.curNode.AppendChild(newNode);
    //                        ctx.curNode = newNode;
    //                    }
    //                }

    //                ctx.newNodeName = "";
    //                ctx.newNodeParamName = "";
    //                ctx.newNodeHasClosingModifier = false;
    //                ctx.newNodeHasSingletonModifier = false;
    //                ctx.newNodeParams = [];
    //                return ParserState.Free;
    //            }).Next()
    //            .Otherwise().Error("Non-tag tokens inside tag")
    //            .WhenEndError("Unexpected EOF");
    //    }

    //    public List<List<ColoredSpan>> Parse(IEnumerable<XmlToken> input)
    //    {
    //        ProcessorCtx ctx = new();
    //        flexer.FlexAll(input, ctx);
    //        if (ctx.curNode.Name != "__root")
    //        {
    //            if (forgiveUnclosedTags)
    //            {
    //                while (ctx.curNode.Name != "__root")
    //                    ctx.curNode = (XmlElement)ctx.curNode.ParentNode!;
    //            }
    //            else throw SyntaxException.FromToken(input.Last(), "Last tag is unclosed");
    //        }
    //        ctx.doc.AppendChild(ctx.curNode);
    //        return ctx.doc;
    //    }

    //    public List<List<ColoredSpan>> Parse(IEnumerable<char> input)
    //    {
    //        return Parse(new XmlTokenizer(keepSpaces).Tokenize(input));
    //    }
    //}

    //internal class HtmlTokenizer
    //{
    //    public enum TokenType
    //    {
    //        Free,
    //        Text,           //token
    //        Space,          //token
    //        EscapeSeq,      //token

    //        TagBegin,       //token

    //        TagCommentBegin1,    //encountered first exclamation mark, expect -
    //        TagCommentBegin2,    //encountered first -, expect second -
    //        TagComment,
    //        TagCommentEnd1,
    //        TagCommentEnd2,

    //        TagWord,        //token
    //        TagModifierClosingTag,      //token
    //        TagModifierSingletonTag,    //token
    //        TagSpace,
    //        TagEquals,
    //        TagParamValue,  //token
    //        TagEnd,         //token
    //    }

    //    readonly TokenizerFlexer flexer;

    //    public HtmlTokenizer(bool keepSpaces = false)
    //    {
    //        flexer = new();
    //        if (keepSpaces)
    //        {
    //            // TokenType.Space should not appear at all
    //            flexer.Register(TokenType.Free)
    //                .When('&').ChangeState(TokenType.EscapeSeq).Skip()
    //                .When('<').ChangeState(TokenType.TagBegin).Skip()
    //                .When('>').Error("Closing tag outside of tag structure")
    //                .Otherwise().ChangeState(TokenType.Text)
    //                .WhenEofDropState();
    //            flexer.Register(TokenType.Text)
    //                .When('&').NextState(TokenType.EscapeSeq).Skip()
    //                .When('<').NextState(TokenType.TagBegin).Skip()
    //                .When('>').Error("Closing tag outside of tag structure")
    //                .Otherwise().Consume()
    //                .WhenEofYieldState();
    //        }
    //        else
    //        {
    //            flexer.Register(TokenType.Free)
    //                .When(CharClass.Whitespace, '\n').ChangeState(TokenType.Space)
    //                .When('&').ChangeState(TokenType.EscapeSeq).Skip()
    //                .When('<').ChangeState(TokenType.TagBegin).Skip()
    //                .When('>').Error("Closing tag outside of tag structure")
    //                .Otherwise().ChangeState(TokenType.Text)
    //                .WhenEofDropState();
    //            flexer.Register(TokenType.Text)
    //                .When(CharClass.Whitespace, '\n').NextState(TokenType.Space)
    //                .When('&').NextState(TokenType.EscapeSeq).Skip()
    //                .When('<').NextState(TokenType.TagBegin).Skip()
    //                .When('>').Error("Closing tag outside of tag structure")
    //                .Otherwise().Consume()
    //                .WhenEofYieldState();
    //            flexer.Register(TokenType.Space)
    //                .When(CharClass.Whitespace, '\n').Skip()
    //                .When('&').NextState(TokenType.EscapeSeq).Skip()
    //                .When('<').NextState(TokenType.TagBegin).Skip()
    //                .When('>').Error("Closing tag outside of tag structure")
    //                .Otherwise().NextState(TokenType.Text)
    //                .WhenEofDropState();
    //        }

    //        flexer.Register(TokenType.EscapeSeq)
    //            .When(';').NextState(TokenType.Free).Skip()
    //            .When(CharClass.Number, CharClass.LatinLetter, '#').Consume()
    //            .Otherwise().Error("Invalid character in escape sequence")
    //            .WhenEofError();

    //        flexer.Register(TokenType.TagBegin)
    //            .When('/').NextState(TokenType.TagModifierClosingTag).Skip()
    //            .When('!').ChangeState(TokenType.TagCommentBegin1).Skip()
    //            .When(CharClass.LatinLetter).NextState(TokenType.TagWord)
    //            .Otherwise().Error("Invalid character in tag")
    //            .WhenEofError();

    //        flexer.Register(TokenType.TagCommentBegin1)
    //            .When('-').ChangeState(FTTokenType.TagCommentBegin2).Skip()
    //            .Otherwise().Error("Invalid character in tag")
    //            .WhenEofError();
    //        flexer.Register(TokenType.TagCommentBegin2)
    //            .When('-').ChangeState(FTTokenType.TagComment).Skip()
    //            .Otherwise().Error("Invalid character in tag")
    //            .WhenEofError();
    //        flexer.Register(TokenType.TagComment)
    //            .When('-').ChangeState(FTTokenType.TagCommentEnd1).Skip()
    //            .Otherwise().Skip()
    //            .WhenEofError();
    //        flexer.Register(FTTokenType.TagCommentEnd1)
    //            .When('-').ChangeState(FTTokenType.TagCommentEnd2).Skip()
    //            .Otherwise().ChangeState(FTTokenType.TagComment).Skip()
    //            .WhenEofError();
    //        flexer.Register(FTTokenType.TagCommentEnd2)
    //            .When('>').ChangeState(FTTokenType.Free).Skip()
    //            .Otherwise().ChangeState(FTTokenType.TagComment).Skip()
    //            .WhenEofError();

    //        flexer.Register(TokenType.TagWord)
    //            .When(CharClass.LatinLetter, CharClass.Number, '_', '-').Consume()
    //            .When(CharClass.Whitespace).NextState(TokenType.TagSpace)
    //            .When('=').NextState(TokenType.TagEquals).Skip()
    //            .When('/').NextState(TokenType.TagModifierSingletonTag).Skip()
    //            .When('>').NextState(TokenType.TagEnd).Skip()
    //            .Otherwise().Error("Invalid character in tag")
    //            .WhenEofError();

    //        flexer.Register(TokenType.TagModifierClosingTag)
    //            .When(CharClass.LatinLetter).NextState(TokenType.TagWord).Consume()
    //            .Otherwise().Error("Invalid character in tag")
    //            .WhenEofError();

    //        flexer.Register(TokenType.TagModifierSingletonTag)
    //            .When('>').NextState(TokenType.TagEnd).Skip()
    //            .Otherwise().Error("Invalid character in tag")
    //            .WhenEofError();
    //        flexer.Register(TokenType.TagSpace)
    //            .When(CharClass.Whitespace, '\n').Skip()
    //            .When(CharClass.LatinLetter).ChangeState(TokenType.TagWord).Consume()
    //            .When('=').ChangeState(TokenType.TagEquals).Skip()
    //            .Otherwise().Error("Invalid character in tag")
    //            .WhenEofError();

    //        flexer.Register(TokenType.TagEquals)
    //            .When('"').ChangeState(TokenType.TagParamValue).Skip()
    //            .Otherwise().Error("Invalid character in tag")
    //            .WhenEofError();

    //        flexer.Register(TokenType.TagParamValue)
    //            .When('"').Skip().NextState(TokenType.TagWord)
    //            .Otherwise().Consume()
    //            .WhenEofError();
    //        flexer.Register(TokenType.TagEnd)
    //            .Otherwise().NextState(TokenType.Free)
    //            .WhenEofYieldState();
    //    }

    //    public List<TokenizerFlexer.Token> Tokenize(IEnumerable<char> input) => flexer.FlexAll(input);
    //}
}
