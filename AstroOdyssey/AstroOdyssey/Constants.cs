using Windows.Foundation;

namespace AstroOdyssey
{
    public static class Constants
    {
        public const string PLAYER = "player";

        public const string ENEMY = "enemy";
        public const string METEOR = "meteor";

        public const string HEALTH = "health";
        public const string POWERUP = "powerup";

        public const string LASER = "laser";

        public const string STAR = "star";

        public enum Difficulty
        {
            Noob,
            StartUp,
            Easy,
            Medium,
            Hard,
            VeryHard,
            Extreme,
            Pro,
        }

        /// <summary>
        /// Checks if a two rects intersect.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static bool Intersects(this Rect source, Rect target)
        {
            var targetX = target.X;
            var targetY = target.Y;
            var sourceX = source.X;
            var sourceY = source.Y;

            var sourceWidth = source.Width - 5;
            var sourceHeight = source.Height - 5;

            var targetWidth = target.Width - 5;
            var targetHeight = target.Height - 5;

            if (source.Width >= 0.0
                && target.Width >= 0.0
                && targetX <= sourceX + sourceWidth
                && targetX + targetWidth >= sourceX
                && targetY <= sourceY + sourceHeight)
            {
                return targetY + targetHeight >= sourceY;
            }

            return false;
        }
    }
}
