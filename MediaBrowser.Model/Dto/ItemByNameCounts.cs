﻿
namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// Class ItemByNameCounts
    /// </summary>
    public class ItemByNameCounts
    {
        /// <summary>
        /// Gets or sets the total count.
        /// </summary>
        /// <value>The total count.</value>
        public int TotalCount { get; set; }
        /// <summary>
        /// Gets or sets the movie count.
        /// </summary>
        /// <value>The movie count.</value>
        public int MovieCount { get; set; }
        /// <summary>
        /// Gets or sets the series count.
        /// </summary>
        /// <value>The series count.</value>
        public int SeriesCount { get; set; }
        /// <summary>
        /// Gets or sets the episode guest star count.
        /// </summary>
        /// <value>The episode guest star count.</value>
        public int EpisodeGuestStarCount { get; set; }
        /// <summary>
        /// Gets or sets the game count.
        /// </summary>
        /// <value>The game count.</value>
        public int GameCount { get; set; }
    }
}
