//
// Copyright (c) Antmicro
// Copyright (c) Realtime Embedded
//
// This file is part of the Emul8 project.
// Full license details are defined in the 'LICENSE' file.
//
using System;
using NUnit.Framework;
using Emul8.UserInterface;
using Emul8.UserInterface.Tokenizer;
using System.Linq;
using Emul8.Exceptions;

namespace MonitorTests
{
    [TestFixture]
    public class TokenizerTests
    {
        [Test]
        public void CommentTest()
        {
            var result = tokenizer.Tokenize("#something");
            AssertTokenizationTypes(result, typeof(CommentToken));
            result = tokenizer.Tokenize("emu[\"SomeIndexWith#Hash\"]");
            AssertTokenizationTypes(result, typeof(LiteralToken), typeof(LeftBraceToken), typeof(StringToken), typeof(RightBraceToken));
            result = tokenizer.Tokenize("emu[\"SomeIndexWithoutHash\"#Comment]");
            AssertTokenizationTypes(result, typeof(LiteralToken), typeof(LeftBraceToken), typeof(StringToken), typeof(CommentToken));
        }

        [Test]
        public void ExecutionTest()
        {
            var result = tokenizer.Tokenize("`something`");
            AssertTokenizationTypes(result, typeof(ExecutionToken));
        }

        [Test]
        public void VariableTest()
        {
            var result = tokenizer.Tokenize("$name $_name $NaMe $.name $123name123");
            AssertTokenizationTypes(result, typeof(VariableToken), typeof(VariableToken), typeof(VariableToken), typeof(VariableToken), typeof(VariableToken));
            result = tokenizer.Tokenize("$variable=\"value\"");
            AssertTokenizationTypes(result, typeof(VariableToken), typeof(EqualToken), typeof(StringToken));
            result = tokenizer.Tokenize("$variable?=\"value\"");
            AssertTokenizationTypes(result, typeof(VariableToken), typeof(ConditionalEqualityToken), typeof(StringToken));
        }

        [Test]
        public void IndexTest()
        {
            var result = tokenizer.Tokenize("emu[\"SomeIndex\"]");
            AssertTokenizationTypes(result, typeof(LiteralToken), typeof(LeftBraceToken), typeof(StringToken), typeof(RightBraceToken));
            result = tokenizer.Tokenize("emu[15]");
            AssertTokenizationTypes(result, typeof(LiteralToken), typeof(LeftBraceToken), typeof(DecimalIntegerToken), typeof(RightBraceToken));
        }

        [Test]
        public void StringTest()
        {
            var result = tokenizer.Tokenize("'string1' \"string2\"");
            AssertTokenizationTypes(result, typeof(StringToken), typeof(StringToken));
            result = tokenizer.Tokenize("'string1\" 'string2\"");
            AssertTokenizationResult(result, 1, null, typeof(StringToken), typeof(LiteralToken));
        }

        [Test]
        public void UnbalancedStringTest()
        {
            var result = tokenizer.Tokenize("\"test\\\"       \\\"         \\\"       test \" 'test\\'        \\'  \\'   test '");
            AssertTokenizationTypes(result, typeof(StringToken), typeof(StringToken));
        }

        [Test]
        public void RangeTest()
        {
            var result = tokenizer.Tokenize("<-5,+5>");
            AssertTokenizationTypes(result, typeof(AbsoluteRangeToken));
            result = tokenizer.Tokenize("<    \t0x123abDE  \t, \t\t  0xabcdef0 \t\t   >");
            AssertTokenizationTypes(result, typeof(AbsoluteRangeToken));
            result = tokenizer.Tokenize("<0xdefg, 0xefgh>");
            AssertTokenizationResult(result, 16);
            result = tokenizer.Tokenize("<5,-6>");
            AssertTokenizationResult(result, 6, typeof(RecoverableException));
        }

        [Test]
        public void RelativeRangeTest()
        {
            var result = tokenizer.Tokenize("<-5,+5>");
            AssertTokenizationTypes(result, typeof(AbsoluteRangeToken));
            result = tokenizer.Tokenize("<0x6 0x2>");
            AssertTokenizationTypes(result, typeof(RelativeRangeToken));
        }

        [Test]
        public void SimplePathTest()
        {
            var result = tokenizer.Tokenize("@Some\\path\\to\\File");
            AssertTokenizationTypes(result, typeof(PathToken));
            result = tokenizer.Tokenize("@Some\\path\\to\\Directory\\");
            AssertTokenizationTypes(result, typeof(PathToken));
        }

        [Test]
        public void EscapedPathTest()
        {
            var result = tokenizer.Tokenize("@Some\\path\\to\\file\\ with\\ Spaces");
            AssertTokenizationTypes(result, typeof(PathToken));
            result = tokenizer.Tokenize("@Some\\path\\to\\directory\\ with\\ Spaces\\");
            AssertTokenizationTypes(result, typeof(PathToken));
        }

        [Test]
        public void MultilineTest()
        {
            var result = tokenizer.Tokenize("\"\"\"");
            AssertTokenizationTypes(result, typeof(MultilineStringTerminatorToken));
            result = tokenizer.Tokenize("\"\"\"SomeMultiline\r\nString with many #tokens [\"inside\"] with\r\nnumbers 123 0x23 and\r\n stuff\"\"\"");
            AssertTokenizationTypes(result, typeof(MultilineStringToken));
        }

        [Test]
        public void BooleanTest()
        {
            var result = tokenizer.Tokenize("true");
            AssertTokenizationTypes(result, typeof(BooleanToken));
            result = tokenizer.Tokenize("TrUe");
            AssertTokenizationTypes(result, typeof(BooleanToken));
            result = tokenizer.Tokenize("FalSE");
            AssertTokenizationTypes(result, typeof(BooleanToken));
            result = tokenizer.Tokenize("false");
            AssertTokenizationTypes(result, typeof(BooleanToken));
        }

        [Test]
        public void IntegerTest()
        {
            var result = tokenizer.Tokenize("123465 -213245 +132432");
            AssertTokenizationTypes(result, typeof(DecimalIntegerToken), typeof(DecimalIntegerToken), typeof(DecimalIntegerToken));
        }

        [Test]
        public void DecimalTest()
        {
            var result = tokenizer.Tokenize("145.5 -.43 +45.");
            AssertTokenizationTypes(result, typeof(FloatToken), typeof(FloatToken), typeof(FloatToken));
        }

        [Test]
        public void HexadecimalTest()
        {
            var result = tokenizer.Tokenize("0xabcdef 0x123469 0xABCDEF 0x123AbC");
            AssertTokenizationTypes(result, typeof(HexToken), typeof(HexToken), typeof(HexToken), typeof(HexToken));
            result = tokenizer.Tokenize("0xgfd 123bcd");
            AssertTokenizationTypes(result, typeof(DecimalIntegerToken), typeof(LiteralToken), typeof(DecimalIntegerToken), typeof(LiteralToken));
        }

        public void LiteralTest()
        {
            var result = tokenizer.Tokenize(".Some.Literal-With?Extra:SignsIn.It:");
            AssertTokenizationTypes(result, typeof(LiteralToken));
        }

        [SetUp]
        public void TestSetUp()
        {
            tokenizer = Tokenizer.CreateTokenizer();
        }

        private static void AssertTokenizationResult(TokenizationResult result, int unmatchedCharacters, Type exception = null, params Type[] types)
        {
            if(exception != null)
            {
                Assert.AreEqual(result.Exception.GetType(), exception);
            }
            else
            {
                Assert.IsNull(result.Exception);
            }
            Assert.IsTrue(result.UnmatchedCharactersLeft == unmatchedCharacters);
            Assert.IsNotNull(result.Tokens);
            var tokens = result.Tokens.ToArray();
            Assert.AreEqual(tokens.Length, types.Length);
            for(var i = 0; i < tokens.Length; ++i)
            {
                Assert.AreSame(tokens[i].GetType(), types[i]);
            }
        }

        private static void AssertTokenizationTypes(TokenizationResult result, params Type[] types)
        {
            Assert.IsNull(result.Exception);
            Assert.IsTrue(result.UnmatchedCharactersLeft == 0);
            Assert.IsNotNull(result.Tokens);
            var tokens = result.Tokens.ToArray();
            Assert.AreEqual(tokens.Length, types.Length);
            for(var i = 0; i < tokens.Length; ++i)
            {
                Assert.AreSame(tokens[i].GetType(), types[i]);
            }
        }

        private Tokenizer tokenizer;

    }
}

