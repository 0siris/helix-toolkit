/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Diagnostics;
using System.Globalization;

namespace HelixToolkit.SharpDX.Core {
    namespace Utilities {
        public class TokenizerHelper {
            private char argSeparator;
            private int charIndex;
            private int currentTokenIndex;
            private int currentTokenLength;
            private char quoteChar;
            private string str;
            private int strLen;

            /// <summary>
            ///     Constructor for TokenizerHelper which accepts an IFormatProvider.
            ///     If the IFormatProvider is null, we use the thread's IFormatProvider info.
            ///     We will use ',' as the list separator, unless it's the same as the
            ///     decimal separator.  If it *is*, then we can't determine if, say, "23,5" is one
            ///     number or two.  In this case, we will use ";" as the separator.
            /// </summary>
            /// <param name="str"> The string which will be tokenized. </param>
            /// <param name="formatProvider"> The IFormatProvider which controls this tokenization. </param>
            public TokenizerHelper(string str, IFormatProvider formatProvider) {
                var numberSeparator = GetNumericListSeparator(formatProvider);
                Initialize(str, '\'', numberSeparator);
            }

            /// <summary>
            ///     Initialize the TokenizerHelper with the string to tokenize,
            ///     the char which represents quotes and the list separator.
            /// </summary>
            /// <param name="str"> The string to tokenize. </param>
            /// <param name="quoteChar"> The quote char. </param>
            /// <param name="separator"> The list separator. </param>
            public TokenizerHelper(string str, char quoteChar, char separator) {
                Initialize(str, quoteChar, separator);
            }

            public bool FoundSeparator { get; private set; }

            /// <summary>
            ///     Initialize the TokenizerHelper with the string to tokenize,
            ///     the char which represents quotes and the list separator.
            /// </summary>
            /// <param name="str"> The string to tokenize. </param>
            /// <param name="quoteChar"> The quote char. </param>
            /// <param name="separator"> The list separator. </param>
            private void Initialize(string str, char quoteChar, char separator) {
                this.str = str;
                strLen = str == null ? 0 : str.Length;
                currentTokenIndex = -1;
                this.quoteChar = quoteChar;
                argSeparator = separator;

                // immediately forward past any whitespace so 
                // NextToken() logic always starts on the first
                // character of the next token.
                while (charIndex < strLen) {
                    if (!char.IsWhiteSpace(this.str, charIndex)) break;

                    ++charIndex;
                }
            }

            public string GetCurrentToken() {
                // if no current token, return null 
                if (currentTokenIndex < 0) return null;

                return str.Substring(currentTokenIndex, currentTokenLength);
            }

            /// <summary>
            ///     Throws an exception if there is any non-whitespace left un-parsed.
            /// </summary>
            public void LastTokenRequired() {
                if (charIndex != strLen) throw new InvalidOperationException("TokenizerHelperExtraDataEncountered");
            }

            /// <summary>
            ///     Advances to the NextToken
            /// </summary>
            /// <returns>true if next token was found, false if at end of string</returns>
            public bool NextToken() {
                return NextToken(false);
            }

            /// <summary>
            ///     Advances to the NextToken, throwing an exception if not present
            /// </summary>
            /// <returns>The next token found</returns>
            public string NextTokenRequired() {
                if (!NextToken(false)) throw new InvalidOperationException("TokenizerHelperPrematureStringTermination");

                return GetCurrentToken();
            }

            /// <summary>
            ///     Advances to the NextToken, throwing an exception if not present
            /// </summary>
            /// <returns>The next token found</returns>
            public string NextTokenRequired(bool allowQuotedToken) {
                if (!NextToken(allowQuotedToken))
                    throw new InvalidOperationException("TokenizerHelperPrematureStringTermination");

                return GetCurrentToken();
            }

            /// <summary>
            ///     Advances to the NextToken
            /// </summary>
            /// <returns>true if next token was found, false if at end of string</returns>
            public bool NextToken(bool allowQuotedToken) {
                // use the currently-set separator character. 
                return NextToken(allowQuotedToken, argSeparator);
            }

            /// <summary>
            ///     Advances to the NextToken.  A separator character can be specified
            ///     which overrides the one previously set.
            /// </summary>
            /// <returns>true if next token was found, false if at end of string</returns>
            public bool NextToken(bool allowQuotedToken, char separator) {
                currentTokenIndex = -1; // reset the currentTokenIndex 
                FoundSeparator = false; // reset

                // If we're at end of the string, just return false.
                if (charIndex >= strLen) return false;

                var currentChar = str[charIndex];

                Debug.Assert(!char.IsWhiteSpace(currentChar), "Token started on Whitespace");

                // setup the quoteCount 
                var quoteCount = 0;

                // If we are allowing a quoted token and this token begins with a quote, 
                // set up the quote count and skip the initial quote
                if (allowQuotedToken &&
                    currentChar == quoteChar) {
                    quoteCount++; // increment quote count
                    ++charIndex;  // move to next character 
                }

                var newTokenIndex = charIndex;
                var newTokenLength = 0;

                // loop until hit end of string or hit a , or whitespace
                // if at end of string ust return false.
                while (charIndex < strLen) {
                    currentChar = str[charIndex];

                    // if have a QuoteCount and this is a quote 
                    // decrement the quoteCount
                    if (quoteCount > 0) {
                        // if anything but a quoteChar we move on
                        if (currentChar == quoteChar) {
                            --quoteCount;

                            // if at zero which it always should for now 
                            // break out of the loop
                            if (0 == quoteCount) {
                                ++charIndex; // move past the quote
                                break;
                            }
                        }
                    } else if (char.IsWhiteSpace(currentChar) || currentChar == separator) {
                        if (currentChar == separator) FoundSeparator = true;
                        break;
                    }

                    ++charIndex;
                    ++newTokenLength;
                }

                // if quoteCount isn't zero we hit the end of the string
                // before the ending quote
                if (quoteCount > 0) throw new InvalidOperationException("TokenizerHelperMissingEndQuote");

                ScanToNextToken(separator); // move so at the start of the nextToken for next call 

                // finally made it, update the _currentToken values
                currentTokenIndex = newTokenIndex;
                currentTokenLength = newTokenLength;

                if (currentTokenLength < 1) throw new InvalidOperationException("TokenizerHelperEmptyToken");

                return true;
            }

            // helper to move the _charIndex to the next token or to the end of the string
            private void ScanToNextToken(char separator) {
                // if already at end of the string don't bother
                if (charIndex < strLen) {
                    var currentChar = str[charIndex];

                    // check that the currentChar is a space or the separator.  If not 
                    // we have an error. this can happen in the quote case
                    // that the char after the quotes string isn't a char. 
                    if (!(currentChar == separator) &&
                        !char.IsWhiteSpace(currentChar))
                        throw new InvalidOperationException("TokenizerHelperExtraDataEncountered");

                    // loop until hit a character that isn't 
                    // an argument separator or whitespace.
                    // !!!Todo: if more than one argSet throw an exception 
                    var argSepCount = 0;
                    while (charIndex < strLen) {
                        currentChar = str[charIndex];

                        if (currentChar == separator) {
                            FoundSeparator = true;
                            ++argSepCount;
                            charIndex++;

                            if (argSepCount > 1) throw new InvalidOperationException("TokenizerHelperEmptyToken");
                        } else if (char.IsWhiteSpace(currentChar)) {
                            ++charIndex;
                        } else {
                            break;
                        }
                    }

                    // if there was a separatorChar then we shouldn't be 
                    // at the end of string or means there was a separator 
                    // but there isn't an arg

                    if (argSepCount > 0 && charIndex >= strLen)
                        throw new InvalidOperationException("TokenizerHelperEmptyToken");
                }
            }

            // Helper to get the numeric list separator for a given IFormatProvider.
            // Separator is a comma [,] if the decimal separator is not a comma, or a semicolon [;] otherwise. 
            public static char GetNumericListSeparator(IFormatProvider provider) {
                var numericSeparator = ',';

                // Get the NumberFormatInfo out of the provider, if possible
                // If the IFormatProvider doesn't not contain a NumberFormatInfo, then 
                // this method returns the current culture's NumberFormatInfo. 
                var numberFormat = NumberFormatInfo.GetInstance(provider);

                Debug.Assert(null != numberFormat);

                // Is the decimal separator is the same as the list separator?
                // If so, we use the ";". 
                if (numberFormat.NumberDecimalSeparator.Length > 0 &&
                    numericSeparator == numberFormat.NumberDecimalSeparator[0]) numericSeparator = ';';

                return numericSeparator;
            }
        }
    }
}
