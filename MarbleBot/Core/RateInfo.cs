using Newtonsoft.Json;

namespace MarbleBot.Core
{
    /// <summary> Stores info about a rating. </summary>
    public readonly struct RateInfo
    {
        /// <summary> What to change the user's input to. </summary>
        public string Input { get; }
        /// <summary> The message of the rating. </summary>
        public string Message { get; }
        /// <summary> The rating out of 10. </summary>
        public int Rating { get; }

        /// <summary> Stores info about a rating. </summary>
        /// <param name="input"> What to change the user's input to. Leave blank for no change. </param>
        /// <param name="message"> The message of the rating. </param>
        /// <param name="rating"> The rating out of 10. Leave blank for a randomised rating. </param>
        [JsonConstructor]
        public RateInfo(string input = null, string message = null, int rating = -3)
        {
            Input = input;
            Message = message;
            Rating = rating;
        }
    }
}
