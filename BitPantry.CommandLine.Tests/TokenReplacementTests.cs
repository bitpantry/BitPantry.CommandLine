using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests
{
    [TestClass]
    public class TokenReplacementTests
    {
        [TestMethod]
        public void ReplaceSingleToken_TokenReplaced()
        {
            var input = "Hello {name}";
            var tokensAndValues = new Dictionary<string, string>
            {
                { "name", "World" }
            };

            var result = TokenReplacement.ReplaceTokens(input, tokensAndValues);
            Assert.AreEqual("Hello World", result);
        }

        [TestMethod]
        public void ReplaceSameTokenTwice_TokensReplaced()
        {
            var input = "Hello {name} {name}";
            var tokensAndValues = new Dictionary<string, string>
            {
                { "name", "World" }
            };

            var result = TokenReplacement.ReplaceTokens(input, tokensAndValues);
            Assert.AreEqual("Hello World World", result);
        }

        [TestMethod]
        public void ReplaceMultipleDifferentTokens_TokensReplaced()
        {
            var input = "Hello {name}, welcome to {place}";
            var tokensAndValues = new Dictionary<string, string>
            {
                { "name", "Alice" },
                { "place", "Wonderland" }
            };

            var result = TokenReplacement.ReplaceTokens(input, tokensAndValues);
            Assert.AreEqual("Hello Alice, welcome to Wonderland", result);
        }

        [TestMethod]
        public void ReplaceTokenWithEmptyString_TokenReplacedWithEmptyString()
        {
            var input = "Hello {name}";
            var tokensAndValues = new Dictionary<string, string>
            {
                { "name", "" }
            };

            var result = TokenReplacement.ReplaceTokens(input, tokensAndValues);
            Assert.AreEqual("Hello ", result);
        }

        [TestMethod]
        public void ReplaceTokenWithNullValue_TokenReplacedWithEmptyString()
        {
            var input = "Hello {name}";
            var tokensAndValues = new Dictionary<string, string>
            {
                { "name", null }
            };

            var result = TokenReplacement.ReplaceTokens(input, tokensAndValues);
            Assert.AreEqual("Hello ", result);
        }

        [TestMethod]
        public void NoTokensInInput_NoReplacement()
        {
            var input = "Hello World";
            var tokensAndValues = new Dictionary<string, string>();

            var result = TokenReplacement.ReplaceTokens(input, tokensAndValues);
            Assert.AreEqual("Hello World", result);
        }

        [TestMethod]
        public void TokenNotInDictionary_TokenNotReplaced()
        {
            var input = "Hello {name}";
            var tokensAndValues = new Dictionary<string, string>
            {
                { "place", "World" }
            };

            var result = TokenReplacement.ReplaceTokens(input, tokensAndValues);
            Assert.AreEqual("Hello {name}", result);
        }

        [TestMethod]
        public void InputIsEmptyString_NoReplacement()
        {
            var input = "";
            var tokensAndValues = new Dictionary<string, string>
            {
                { "name", "World" }
            };

            var result = TokenReplacement.ReplaceTokens(input, tokensAndValues);
            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void TokensWithSpecialCharacters_TokensReplaced()
        {
            var input = "Hello {na-me}, welcome to {pla_ce}";
            var tokensAndValues = new Dictionary<string, string>
            {
                { "na-me", "Alice" },
                { "pla_ce", "Wonderland" }
            };

            var result = TokenReplacement.ReplaceTokens(input, tokensAndValues);
            Assert.AreEqual("Hello Alice, welcome to Wonderland", result);
        }
    }
}
