using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using static AstroOdyssey.Constants;

namespace AstroOdyssey
{
    public partial class GamePage : Page
    {
        #region Fields

        private string baseUrl;

        //private const float FRAME_CAP_MS = 1000.0f / 60.0f;

        private int fpsCount;
        private int fpsCounter;
        private int frameStatUpdateCounter;
        private int frameStatUpdateLimit;

        private float lastFrameTime;
        private long frameStartTime;

        //private long frameTime;
        private int frameDuration = 10;

        private bool gameIsRunning;

        private int enemyCounter;
        private int enemySpawnLimit;
        private double enemySpeed;

        private int enemySpawnCounter;

        private int meteorCounter;
        private int meteorSpawnLimit;
        private double meteorSpeed;

        private int healthCounter;
        private int healthSpawnLimit;
        private double healthSpeed;

        private int powerUpCounter;
        private int powerUpSpawnLimit;
        private double powerUpSpeed;

        private int starCounter;
        private int starSpawnLimit;
        private double starSpeed;

        private double playerSpeed;

        private double score;

        private double pointerX;
        private double windowWidth, windowHeight;

        private int laserCounter;
        private int laserSpawnLimit;
        private double laserSpeed;

        private bool powerUpTriggered;
        private int powerUpTriggerCounter;
        private int powerUpTriggerLimit;

        private object backgroundAudio = null;

        private object enemyDestructionAudio = null;
        private object meteorDestructionAudio = null;

        private object playerHealthLossAudio = null;
        private object playerHealthGainAudio = null;

        private object levelUpAudio = null;

        private object powerUpAudio = null;
        private object powerDownAudio = null;

        private object laserImpactAudio = null;
        private object laserAudio = null;
        private object laserPoweredUpModeAudio = null;

        private Difficulty difficulty;

        private int showInGameTextCount;
        private int showInGameTextLimit;

        private readonly Random random = new Random();

        private readonly Stack<GameObject> enemyStack = new Stack<GameObject>();
        private readonly Stack<GameObject> meteorStack = new Stack<GameObject>();
        private readonly Stack<GameObject> healthStack = new Stack<GameObject>();
        private readonly Stack<GameObject> powerUpStack = new Stack<GameObject>();
        private readonly Stack<GameObject> starStack = new Stack<GameObject>();

        private Player player;

        private int playerDamagedOpacityCount;
        private int playerDamagedOpacityLimit;

        private bool moveLeft = false, moveRight = false;

        #endregion

        #region Ctor

        public GamePage()
        {
            InitializeComponent();

            Loaded += GamePage_Loaded;
            Unloaded += GamePage_Unloaded;

            SetWindowSize();
        }

        #endregion

        #region Methods

        #region Frame Methods

        /// <summary>
        /// Updates a frame of the game.
        /// </summary>
        private void UpdateFrame()
        {
            UpdateGameStats();

            UpdateFrameStats();

            SpawnEnemy();

            SpawnMeteor();

            SpawnHealth();

            SpawnPowerUp();

            SpawnStar();

            SpawnLaser(powerUpTriggered);

            MovePlayer();

            UpdateGameView();

            UpdateStarView();

            ShiftDifficulty();

            HideInGameText();

            TriggerPowerDown();

            PlayerOpacity();

            CheckPlayerDeath();

            CalculateFps();

            KeyboardFocus();
        }

        /// <summary>
        /// Updates the fps, frame time and objects currently in view.
        /// </summary>
        private void UpdateFrameStats()
        {
            frameStatUpdateCounter -= 1;

            if (frameStatUpdateCounter < 0)
            {
                FPSText.Text = "FPS: " + fpsCount;
#if DEBUG
                FrameDurationText.Text = "Frame duration: " + frameDuration + "ms";
                ObjectsCountText.Text = "Objects count: " + GameView.Children.Count();
#endif

                frameStatUpdateCounter = frameStatUpdateLimit;
            }
        }

        /// <summary>
        /// Elapses the frame duration.
        /// </summary>
        /// <param name="watch"></param>        
        /// <returns></returns>
        private async Task ElapseFrameDuration(Stopwatch watch)
        {
            //CalculateFrameDuration(watch);
            await Task.Delay(frameDuration);
        }

        ///// <summary>
        ///// Calculates the duration of a frame.
        ///// </summary>
        ///// <param name="watch"></param>
        ///// <returns></returns>
        //private void CalculateFrameDuration(Stopwatch watch)
        //{
        //    frameTime = watch.ElapsedMilliseconds - frameStartTime;
        //    frameDuration = Math.Max((int)(FRAME_CAP_MS - frameTime), 1);
        //}

        /// <summary>
        /// Calculates frames per second.
        /// </summary>
        /// <param name="frameStartTime"></param>
        private void CalculateFps()
        {
            // calculate FPS
            if (lastFrameTime + 1000 < frameStartTime)
            {
                fpsCount = fpsCounter;
                fpsCounter = 0;
                lastFrameTime = frameStartTime;
            }

            fpsCounter++;
        }

        #endregion

        #region Game Methods

        /// <summary>
        /// Starts the game. Spawns the player and starts game and laser loops.
        /// </summary>
        private void StartGame()
        {
            PlayBackgroundMusic();
            SetDefaultGameEnvironment();
            SpawnPlayer();
            SpawnStar();

            gameIsRunning = true;

            RunGameLoop();
        }

        /// <summary>
        /// Sets the game environment to it's default state.
        /// </summary>
        private void SetDefaultGameEnvironment()
        {
            enemyCounter = 100;
            enemySpawnLimit = 45;
            enemySpeed = 3;

            meteorCounter = 100;
            meteorSpawnLimit = 50;
            meteorSpeed = 2;

            healthCounter = 1000;
            healthSpawnLimit = 1000;
            healthSpeed = 2;

            powerUpCounter = 1000;
            powerUpSpawnLimit = 1000;
            powerUpSpeed = 2;

            starCounter = 100;
            starSpawnLimit = 100;
            starSpeed = 0.1d;

            playerSpeed = 15;
            playerDamagedOpacityLimit = 100;

            score = 0;

            fpsCount = 0;
            fpsCounter = 0;
            lastFrameTime = 0;
            frameStatUpdateLimit = 5;

            laserSpawnLimit = 15;
            laserSpeed = 20;
            powerUpTriggerLimit = 500;

            difficulty = Difficulty.Noob;

            showInGameTextLimit = 100;

            GameView.Children.Clear();
        }

        /// <summary>
        /// Stops the game.
        /// </summary>
        private void StopGame()
        {
            StopBackgroundMusic();
            gameIsRunning = false;
        }

        /// <summary>
        /// Runs the game loop if game is running. Updates stats, gets player bounds, spawns enemies and meteors, moves the player, updates the frame, scales difficulty, checks player health, calculates fps and frame time.
        /// </summary>
        private async void RunGameLoop()
        {
            var watch = Stopwatch.StartNew();

            while (gameIsRunning)
            {
                frameStartTime = watch.ElapsedMilliseconds;

                UpdateFrame();

                await ElapseFrameDuration(watch);
            }
        }

        /// <summary>
        /// Brings focus on keyboard so that keyboard events work.
        /// </summary>
        private void KeyboardFocus()
        {
            FocusBox.Focus();
        }

        /// <summary>
        /// Updates the game score, player health.
        /// </summary>
        private void UpdateGameStats()
        {
            ScoreText.Text = "Score: " + score;
            HealthText.Text = player.GetHealthPoints();
        }

        /// <summary>
        /// Shows the level up text in game view.
        /// </summary>
        private void ShowLevelUp()
        {
            ShowInGameText("LEVEL UP!");
            PlayLevelUpSound();
        }

        /// <summary>
        /// Shows the in game text in game view.
        /// </summary>
        private void ShowInGameText(string text)
        {
            InGameText.Text = text;
            InGameText.Visibility = Visibility.Visible;
            showInGameTextCount = showInGameTextLimit;
        }

        /// <summary>
        /// Hides the in game text after keeping it visible.
        /// </summary>
        private void HideInGameText()
        {
            if (InGameText.Visibility == Visibility.Visible)
            {
                showInGameTextCount -= 1;

                if (showInGameTextCount <= 0)
                {
                    InGameText.Visibility = Visibility.Collapsed;
                }
            }
        }

        ///// <summary>
        ///// Checks if a two rects intersect.
        ///// </summary>
        ///// <param name="source"></param>
        ///// <param name="target"></param>
        ///// <returns></returns>
        //private bool RectsIntersect(Rect source, Rect target)
        //{
        //    var targetX = target.X;
        //    var targetY = target.Y;
        //    var sourceX = source.X;
        //    var sourceY = source.Y;

        //    var sourceWidth = source.Width - 5;
        //    var sourceHeight = source.Height - 5;

        //    var targetWidth = target.Width - 5;
        //    var targetHeight = target.Height - 5;

        //    if (source.Width >= 0.0
        //        && target.Width >= 0.0
        //        && targetX <= sourceX + sourceWidth
        //        && targetX + targetWidth >= sourceX
        //        && targetY <= sourceY + sourceHeight)
        //    {
        //        return targetY + targetHeight >= sourceY;
        //    }

        //    return false;
        //}

        /// <summary>
        /// Sets the window and canvas size on startup.
        /// </summary>
        private void SetWindowSize()
        {
            windowWidth = Window.Current.Bounds.Width;
            windowHeight = Window.Current.Bounds.Height;

            pointerX = windowWidth / 2;

            SetCanvasSize();
        }

        /// <summary>
        /// Sets the game canvas size according to current window size.
        /// </summary>
        private void SetCanvasSize()
        {
            GameView.SetSize(windowHeight, windowWidth);
            StarView.SetSize(windowHeight, windowWidth);
        }

        /// <summary>
        /// Updates a frame of game view. Advances game objects in the frame.
        /// </summary>
        private void UpdateGameView()
        {
            var gameObjects = GameView.GetGameObjects<GameObject>().Where(x => x is not Player);

            foreach (var gameObject in gameObjects)
            {
                UpdateGameViewObjects(gameObject);
            }

            ClearGameView();
        }

        /// <summary>
        /// Updates game objects in game view. Moves the objects. Detects collision causes and applies effects.
        /// </summary>
        /// <param name="gameObject"></param>
        private void UpdateGameViewObjects(GameObject gameObject)
        {
            if (gameObject.MarkedForFadedRemoval)
            {
                gameObject.Fade();

                if (gameObject.HasFadedAway)
                {
                    GameView.AddDestroyableGameObject(gameObject);
                    return;
                }
            }

            var tag = gameObject.Tag;

            switch (tag)
            {
                case Constants.ENEMY:
                    {
                        var enemy = gameObject as Enemy;

                        // move enemy down
                        enemy.MoveY();
                        enemy.MoveX();

                        // if the object is marked for lazy destruction then no need to perform collisions
                        if (enemy.MarkedForFadedRemoval)
                            return;

                        // if enemy or meteor object has gone beyond game view
                        if (RemoveGameObject(enemy))
                            return;

                        // check if enemy collides with player
                        if (PlayerCollision(enemy))
                            return;

                        // perform laser collisions
                        LaserCollision(enemy);
                    }
                    break;
                case Constants.METEOR:
                    {
                        var meteor = gameObject as Meteor;

                        // move meteor down
                        meteor.Rotate();
                        meteor.MoveY();

                        // if the object is marked for lazy destruction then no need to perform collisions
                        if (meteor.MarkedForFadedRemoval)
                            return;

                        // if enemy or meteor object has gone beyond game view
                        if (RemoveGameObject(meteor))
                            return;

                        // check if meteor collides with player
                        if (PlayerCollision(meteor))
                            return;

                        // perform laser collisions
                        LaserCollision(meteor);
                    }
                    break;
                case Constants.LASER:
                    {
                        var laser = gameObject as Laser;

                        // move laser up                
                        laser.MoveY();

                        // remove laser if outside game canvas
                        if (laser.GetY() < 10)
                            GameView.AddDestroyableGameObject(laser);
                    }
                    break;
                case Constants.HEALTH:
                    {
                        var health = gameObject as Health;

                        // move Health down
                        health.MoveY();

                        // if health object has gone below game view
                        if (health.GetY() > GameView.Height)
                            GameView.AddDestroyableGameObject(health);

                        if (player.GetRect().Intersects(health.GetRect()))
                        {
                            GameView.AddDestroyableGameObject(health);
                            PlayerHealthGain(health);
                        }
                    }
                    break;
                case Constants.POWERUP:
                    {
                        var powerUp = gameObject as PowerUp;

                        // move PowerUp down
                        powerUp.MoveY();

                        // if PowerUp object has gone below game view
                        if (powerUp.GetY() > GameView.Height)
                            GameView.AddDestroyableGameObject(powerUp);

                        if (player.GetRect().Intersects(powerUp.GetRect()))
                        {
                            GameView.AddDestroyableGameObject(powerUp);
                            TriggerPowerUp();
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Clears destroyable objects from game view.
        /// </summary>
        private void ClearGameView()
        {
            foreach (var destroyable in GameView.GetDestroyableGameObjects())
            {
                if (destroyable is Enemy enemy)
                    enemyStack.Push(enemy);

                if (destroyable is Meteor meteor)
                    meteorStack.Push(meteor);

                if (destroyable is Health health)
                    healthStack.Push(health);

                GameView.RemoveGameObject(destroyable);
            }

            GameView.ClearDestroyableGameObjects();
        }

        /// <summary>
        /// Removes a game object from game view. 
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        private bool RemoveGameObject(GameObject gameObject)
        {
            if (gameObject.GetY() > GameView.Height || gameObject.GetX() > GameView.Width || gameObject.GetX() + gameObject.Width < 10)
            {
                GameView.AddDestroyableGameObject(gameObject);

                return true;
            }

            return false;
        }

        #endregion

        #region Score Methods

        /// <summary>
        /// Increase player score if an enemy was destroyed.
        /// </summary>
        private void PlayerScoreByEnemyDestruction()
        {
            score += 2;
        }

        /// <summary>
        /// Increase player score if a meteor was destroyed.
        /// </summary>
        private void PlayerScoreByMeteorDestruction()
        {
            score++;
        }

        #endregion

        #region Difficulty Methods

        /// <summary>
        /// Shifts the difficulty of the game according to score; 
        /// </summary>
        private void ShiftDifficulty()
        {
            var currentDifficulty = difficulty;

            if (score > 0)
                difficulty = Difficulty.Noob;
            if (score > 25)
                difficulty = Difficulty.StartUp;
            if (score > 50)
                difficulty = Difficulty.Easy;
            if (score > 100)
                difficulty = Difficulty.Medium;
            if (score > 200)
                difficulty = Difficulty.Hard;
            if (score > 400)
                difficulty = Difficulty.VeryHard;
            if (score > 800)
                difficulty = Difficulty.Extreme;
            if (score > 1600)
                difficulty = Difficulty.Pro;

            // when difficulty changes show level up
            if (currentDifficulty != difficulty)
            {
                ShowLevelUp();
                ScaleDifficulty();
            }
        }

        /// <summary>
        /// Scales up difficulty according to player score.
        /// </summary>
        private void ScaleDifficulty()
        {
            switch (difficulty)
            {
                case Difficulty.Noob:
                    {
                        // Do nothing.
                    }
                    break;
                case Difficulty.StartUp:
                    {
                        meteorSpawnLimit -= 5;
                        laserSpawnLimit -= 1;
                    }
                    break;
                default:
                    {
                        enemySpawnLimit -= 5;
                        enemySpeed += 2;

                        meteorSpawnLimit -= 5;
                        meteorSpeed += 2;

                        laserSpawnLimit -= 1;

                        healthSpeed += 2;
                        powerUpSpeed += 2;

                        starSpeed += 0.1d;
                    }
                    break;
            }
        }

        #endregion

        #region Player Methods

        /// <summary>
        /// Spawns the player.
        /// </summary>
        private void SpawnPlayer()
        {
            player = new Player();
            player.SetAttributes(playerSpeed);
            player.AddToGameEnvironment(top: windowHeight - player.Height - 20, left: pointerX, gameEnvironment: GameView);
        }

        /// <summary>
        /// Sets the y axis position of the player on game canvas.
        /// </summary>
        private void SetPlayerY()
        {
            player.SetY(windowHeight - player.Height - 20);
        }

        /// <summary>
        /// Sets the x axis position of the player on game canvas.
        /// </summary>
        /// <param name="x"></param>
        private void SetPlayerX(double x)
        {
            player.SetX(x);
        }

        /// <summary>
        /// Moves the player to last pointer pressed position by x axis.
        /// </summary>
        private void MovePlayer()
        {
            var playerX = player.GetX();
            var playerWidthHalf = player.Width / 2;

            if (moveLeft && playerX > 0)
                pointerX -= player.Speed;

            if (moveRight && playerX + player.Width < windowWidth)
                pointerX += player.Speed;

            // move right
            if (pointerX - playerWidthHalf > playerX + player.Speed)
            {
                if (playerX + playerWidthHalf < windowWidth)
                {
                    SetPlayerX(playerX + player.Speed);
                }
            }

            // move left
            if (pointerX - playerWidthHalf < playerX - player.Speed)
            {
                SetPlayerX(playerX - player.Speed);
            }
        }

        ///// <summary>
        ///// Gets the player health points.
        ///// </summary>
        ///// <returns></returns>
        //private string GetPlayerHealthPoints()
        //{
        //    var healthPoints = player.Health / player.HealthSlot;
        //    var healthIcon = "❤️";
        //    var health = string.Empty;

        //    for (int i = 0; i < healthPoints; i++)
        //    {
        //        health += healthIcon;
        //    }

        //    return health;
        //}

        /// <summary>
        /// Check if player is dead.
        /// </summary>
        private void CheckPlayerDeath()
        {
            // game over
            if (player.HasNoHealth)
            {
                HealthText.Text = "Game Over";
                StopGame();

                var contentDialogue = new MessageDialogueWindow(title: "GAME OVER", message: "Would you like to play again?", result: (result) =>
                {
                    if (result)
                        App.NavigateToPage("/GamePage");
                    else
                        App.NavigateToPage("/GameStartPage");
                });
                contentDialogue.Show();
            }
        }

        /// <summary>
        /// Makes the player loose health.
        /// </summary>
        private void PlayerHealthLoss()
        {
            player.LooseHealth();

            PlayPlayerHealthLossSound();

            player.Opacity = 0.4d;

            playerDamagedOpacityCount = playerDamagedOpacityLimit;
        }

        /// <summary>
        /// Sets the player opacity.
        /// </summary>
        private void PlayerOpacity()
        {
            playerDamagedOpacityCount -= 1;

            if (playerDamagedOpacityCount <= 0)
            {
                player.Opacity = 1;
            }
        }

        /// <summary>
        /// Makes the player gain health.
        /// </summary>
        /// <param name="health"></param>
        private void PlayerHealthGain(Health health)
        {
            player.GainHealth(health.Health);

            PlayPlayerHealthGainSound();
        }

        /// <summary>
        /// Checks and performs player collision.
        /// </summary>
        /// <param name="gameObject"></param>
        /// 
        /// <returns></returns>
        private bool PlayerCollision(GameObject gameObject)
        {
            if (player.GetRect().Intersects(gameObject.GetRect()))
            {
                GameView.AddDestroyableGameObject(gameObject);
                PlayerHealthLoss();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Plays the sound effect when the player looses health.
        /// </summary>
        private void PlayPlayerHealthLossSound()
        {
            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/explosion-39897.mp3";

            if (playerHealthLossAudio is null)
                playerHealthLossAudio = OpenSilver.Interop.ExecuteJavaScript(@"
                (function() {
                    //play audio with out html audio tag
                    var playerHealthDecreaseAudio = new Audio($0);
                    playerHealthDecreaseAudio.volume = 1.0;
                   return playerHealthDecreaseAudio;
                }())", host);

            AudioService.PlayAudio(playerHealthLossAudio);
        }

        /// <summary>
        /// Plays the sound effect when the player gains health.
        /// </summary>
        private void PlayPlayerHealthGainSound()
        {
            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/scale-e6-14577.mp3";

            if (playerHealthGainAudio is null)
                playerHealthGainAudio = OpenSilver.Interop.ExecuteJavaScript(@"
                (function() {
                    //play audio with out html audio tag
                    var playerHealthGainAudio = new Audio($0);
                    playerHealthGainAudio.volume = 1.0;
                   return playerHealthGainAudio;
                }())", host);

            AudioService.PlayAudio(playerHealthGainAudio);
        }

        /// <summary>
        /// Plays the level up audio.
        /// </summary>
        private void PlayLevelUpSound()
        {
            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/8-bit-powerup-6768.mp3";

            if (levelUpAudio is null)
                levelUpAudio = OpenSilver.Interop.ExecuteJavaScript(@"
                (function() {
                    //play audio with out html audio tag
                    var levelUpAudio = new Audio($0);
                    levelUpAudio.volume = 1.0;
                   return levelUpAudio;
                }())", host);

            AudioService.PlayAudio(levelUpAudio);
        }

        #endregion

        #region Laser Methods        

        /// <summary>
        /// Spawns a laser.
        /// </summary>
        private void SpawnLaser(bool isPoweredUp)
        {
            // each frame progress decreases this counter
            laserCounter -= 1;

            if (laserCounter <= 0)
            {
                // any object falls within player range
                if (GameView.GetGameObjects<GameObject>().Where(x => x.IsDestructible).Any(x => player.AnyNearbyObjectsOnTheRight(gameObject: x) || player.AnyNearbyObjectsOnTheLeft(gameObject: x)))
                {
                    double laserHeight = 0, laserWidth = 0;

                    switch (difficulty)
                    {
                        case Difficulty.Noob:
                            { laserHeight = 20; laserWidth = 5; }
                            break;
                        case Difficulty.StartUp:
                            { laserHeight = 25; laserWidth = 10; }
                            break;
                        case Difficulty.Easy:
                            { laserHeight = 30; laserWidth = 15; }
                            break;
                        case Difficulty.Medium:
                            { laserHeight = 35; laserWidth = 20; }
                            break;
                        case Difficulty.Hard:
                            { laserHeight = 40; laserWidth = 25; }
                            break;
                        case Difficulty.VeryHard:
                            { laserHeight = 45; laserWidth = 30; }
                            break;
                        case Difficulty.Extreme:
                            { laserHeight = 50; laserWidth = 35; }
                            break;
                        case Difficulty.Pro:
                            { laserHeight = 55; laserWidth = 40; }
                            break;
                        default:
                            break;
                    }

                    GenerateLaser(laserHeight: laserHeight, laserWidth: laserWidth, isPoweredUp: isPoweredUp);
                }

                laserCounter = laserSpawnLimit;
            }
        }

        /// <summary>
        /// Generates a laser.
        /// </summary>
        /// <param name="laserHeight"></param>
        /// <param name="laserWidth"></param>
        private void GenerateLaser(double laserHeight, double laserWidth, bool isPoweredUp)
        {
            var newLaser = new Laser();

            newLaser.SetAttributes(speed: laserSpeed, height: laserHeight, width: laserWidth, isPoweredUp: isPoweredUp);

            newLaser.AddToGameEnvironment(top: player.GetY() - 20, left: player.GetX() + player.Width / 2 - newLaser.Width / 2, gameEnvironment: GameView);

            if (newLaser.IsPoweredUp)
                PlayLaserPoweredUpModeSound();
            else
                PlayLaserSound();
        }

        /// <summary>
        /// Checks and performs laser collision.
        /// </summary>
        /// <param name="gameObject"></param>
        /// 
        private void LaserCollision(GameObject gameObject)
        {
            var lasers = GameView.GetGameObjects<Laser>().Where(laser => laser.GetRect().Intersects(gameObject.GetRect()));

            if (lasers is not null && lasers.Any())
            {
                foreach (var laser in lasers)
                {
                    GameView.AddDestroyableGameObject(laser);

                    // if laser is powered up then execute one shot kill
                    if (laser.IsPoweredUp)
                        gameObject.LooseHealth(gameObject.Health);
                    else
                        gameObject.LooseHealth();

                    // move the enemy backwards a bit on laser hit
                    gameObject.MoveY(gameObject.Speed * 3 / 2, YDirection.UP);
                    
                    PlayLaserImpactSound();

                    if (gameObject.HasNoHealth)
                    {
                        switch (gameObject.Tag)
                        {
                            case Constants.ENEMY:
                                {
                                    DestroyEnemy(gameObject as Enemy);
                                }
                                break;
                            case Constants.METEOR:
                                {
                                    DestroyMeteor(gameObject as Meteor);
                                }
                                break;
                            default:
                                break;
                        }
                    }                    
                }
            }
        }

        /// <summary>
        /// Plays the sound effect when a laser hits a meteor or an enemy.
        /// </summary>
        private void PlayLaserImpactSound()
        {
            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/explosion-sfx-43814.mp3";

            if (laserImpactAudio is null)
                laserImpactAudio = OpenSilver.Interop.ExecuteJavaScript(@"
                (function() {
                    //play audio with out html audio tag
                    var laserImpactAudio = new Audio($0);
                    laserImpactAudio.volume = 0.6;
                    return laserImpactAudio;
                }())", host);

            AudioService.PlayAudio(laserImpactAudio);
        }

        /// <summary>
        /// Plays the laser sound efect.
        /// </summary>
        private void PlayLaserSound()
        {
            //TODO: change default laser audio

            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/shoot02wav-14562.mp3";

            if (laserAudio is null)
                laserAudio = OpenSilver.Interop.ExecuteJavaScript(@"
                (function() {
                    //play audio with out html audio tag
                    var laserAudio = new Audio($0);
                    laserAudio.volume = 0.4;
                    return laserAudio;
                }())", host);

            AudioService.PlayAudio(laserAudio);
        }

        /// <summary>
        /// Plays the laser power up sound effect.
        /// </summary>
        private void PlayLaserPoweredUpModeSound()
        {
            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/plasmablaster-37114.mp3";

            if (laserPoweredUpModeAudio is null)
                laserPoweredUpModeAudio = OpenSilver.Interop.ExecuteJavaScript(@"
                (function() {
                    //play audio with out html audio tag
                    var laserPoweredUpAudio = new Audio($0);
                    laserPoweredUpAudio.volume = 1;
                    return laserPoweredUpAudio;
                }())", host);

            AudioService.PlayAudio(laserPoweredUpModeAudio);
        }

        #endregion

        #region Enemy Methods

        /// <summary>
        /// Spawns an enemy.
        /// </summary>
        private void SpawnEnemy()
        {
            // each frame progress decreases this counter
            enemyCounter -= 1;

            // when counter reaches zero, create an enemy
            if (enemyCounter < 0)
            {
                GenerateEnemy();

                enemySpawnCounter++;

                enemyCounter = enemySpawnLimit;
            }
        }

        /// <summary>
        /// Generates a random enemy.
        /// </summary>
        private void GenerateEnemy()
        {
            var newEnemy = enemyStack.Any() ? enemyStack.Pop() as Enemy : new Enemy();

            newEnemy.SetAttributes(enemySpeed + random.Next(0, 4));

            var left = random.Next(10, (int)windowWidth - 100);
            var top = 0 - newEnemy.Height;

            // when not noob anymore enemy moves sideways
            if ((int)difficulty > 0 && enemySpawnCounter >= 10)
            {
                newEnemy.XDirection = (XDirection)random.Next(1, 3);
                enemySpawnCounter = 0;
            }

            newEnemy.AddToGameEnvironment(top: top, left: left, gameEnvironment: GameView);
        }

        /// <summary>
        /// Destroys an enemy. Removes from game environment, increases player score, plays sound effect.
        /// </summary>
        /// <param name="meteor"></param>
        private void DestroyEnemy(Enemy enemy)
        {
            enemy.MarkedForFadedRemoval = true;

            PlayerScoreByEnemyDestruction();

            PlayEnemyDestructionSound();
        }

        /// <summary>
        /// Plays the enemy destruction sound effect.
        /// </summary>
        private void PlayEnemyDestructionSound()
        {
            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/explosion-36210.mp3";

            if (enemyDestructionAudio is null)
                enemyDestructionAudio = OpenSilver.Interop.ExecuteJavaScript(@"
                (function() {
                    //play audio with out html audio tag
                    var enemyDestructionAudio = new Audio($0);
                    enemyDestructionAudio.volume = 0.8;                
                    return enemyDestructionAudio;
                }())", host);

            AudioService.PlayAudio(enemyDestructionAudio);
        }

        #endregion

        #region Meteor Methods

        /// <summary>
        /// Spawns a meteor.
        /// </summary>
        private void SpawnMeteor()
        {
            if ((int)difficulty > 0)
            {
                // each frame progress decreases this counter
                meteorCounter -= 1;

                // when counter reaches zero, create a meteor
                if (meteorCounter < 0)
                {
                    GenerateMeteor();
                    meteorCounter = meteorSpawnLimit;
                }
            }
        }

        /// <summary>
        /// Generates a random meteor.
        /// </summary>
        private void GenerateMeteor()
        {
            var newMeteor = meteorStack.Any() ? meteorStack.Pop() as Meteor : new Meteor();

            newMeteor.SetAttributes(meteorSpeed + random.NextDouble());
            newMeteor.AddToGameEnvironment(top: 0 - newMeteor.Height, left: random.Next(10, (int)windowWidth - 100), gameEnvironment: GameView);
        }

        /// <summary>
        /// Destroys a meteor. Removes from game environment, increases player score, plays sound effect.
        /// </summary>
        /// <param name="meteor"></param>
        private void DestroyMeteor(Meteor meteor)
        {
            meteor.MarkedForFadedRemoval = true;

            PlayerScoreByMeteorDestruction();

            PlayMeteorDestructionSound();
        }

        /// <summary>
        /// Plays the meteor destruction sound effect.
        /// </summary>
        private void PlayMeteorDestructionSound()
        {
            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/explosion-36210.mp3";

            if (meteorDestructionAudio is null)
                meteorDestructionAudio = OpenSilver.Interop.ExecuteJavaScript(@"
                (function() {
                    //play audio with out html audio tag
                    var meteorDestructionAudio = new Audio($0);
                    meteorDestructionAudio.volume = 0.8;
                    return meteorDestructionAudio;
                }())", host);

            AudioService.PlayAudio(meteorDestructionAudio);
        }

        #endregion

        #region Health Methods

        /// <summary>
        /// Spawns a Health.
        /// </summary>
        private void SpawnHealth()
        {
            if (player.Health <= 60)
            {
                // each frame progress decreases this counter
                healthCounter -= 1;

                // when counter reaches zero, create a Health
                if (healthCounter < 0)
                {
                    GenerateHealth();
                    healthCounter = healthSpawnLimit;
                }
            }
        }

        /// <summary>
        /// Generates a random Health.
        /// </summary>
        private void GenerateHealth()
        {
            var newHealth = healthStack.Any() ? healthStack.Pop() as Health : new Health();

            newHealth.SetAttributes(healthSpeed + random.NextDouble());
            newHealth.AddToGameEnvironment(top: 0 - newHealth.Height, left: random.Next(10, (int)windowWidth - 100), gameEnvironment: GameView);
        }

        #endregion

        #region PowerUp Methods

        /// <summary>
        /// Spawns a PowerUp.
        /// </summary>
        private void SpawnPowerUp()
        {
            // each frame progress decreases this counter
            powerUpCounter -= 1;

            // when counter reaches zero, create a PowerUp
            if (powerUpCounter < 0)
            {
                GeneratePowerUp();
                powerUpCounter = powerUpSpawnLimit;
            }

        }

        /// <summary>
        /// Generates a random PowerUp.
        /// </summary>
        private void GeneratePowerUp()
        {
            var newPowerUp = powerUpStack.Any() ? powerUpStack.Pop() as PowerUp : new PowerUp();

            newPowerUp.SetAttributes(powerUpSpeed + random.NextDouble());
            newPowerUp.AddToGameEnvironment(top: 0 - newPowerUp.Height, left: random.Next(10, (int)windowWidth - 100), gameEnvironment: GameView);
        }

        /// <summary>
        /// Triggers the powered up state.
        /// </summary>
        private void TriggerPowerUp()
        {
            powerUpTriggered = true;
            powerUpTriggerCounter = powerUpTriggerLimit;

            laserSpawnLimit -= 1;
            ShowInGameText("POWER UP!");

            PlayPowerUpSound();
            player.TriggerPowerUp();
        }

        /// <summary>
        /// Triggers the powered up state down.
        /// </summary>
        private void TriggerPowerDown()
        {
            if (powerUpTriggered)
            {
                powerUpTriggerCounter -= 1;

                var powerGauge = (double)(powerUpTriggerCounter / 100) + 1;

                player.SetPowerGauge(powerGauge);

                if (powerUpTriggerCounter <= 0)
                {
                    powerUpTriggered = false;
                    laserSpawnLimit += 1;

                    PlayPowerDownSound();
                    player.TriggerPowerDown();
                }
            }
        }

        #endregion

        #region Star Methods

        /// <summary>
        /// Spawns random stars in the star view.
        /// </summary>
        private void SpawnStar()
        {
            // each frame progress decreases this counter
            starCounter -= 1;

            // when counter reaches zero, create an star
            if (starCounter < 0)
            {
                GenerateStar();
                starCounter = starSpawnLimit;
            }
        }

        /// <summary>
        /// Generates a random star.
        /// </summary>
        private void GenerateStar()
        {
            var newStar = starStack.Any() ? starStack.Pop() as Star : new Star();

            newStar.SetAttributes(starSpeed);
            newStar.AddToGameEnvironment(top: 0 - newStar.Height, left: random.Next(10, (int)windowWidth - 10), gameEnvironment: StarView);
        }

        /// <summary>
        /// Updates stars on the star canvas.
        /// </summary>
        private void UpdateStarView()
        {
            var starObjects = StarView.GetGameObjects<GameObject>();

            foreach (var star in starObjects)
            {
                UpdateStarViewObject(star);
            }

            ClearStarView();
        }

        /// <summary>
        /// Updates the star objects. Moves the stars.
        /// </summary>
        /// <param name="star"></param>
        private void UpdateStarViewObject(GameObject star)
        {
            // move star down
            star.MoveY(starSpeed);

            if (star.GetY() > windowHeight)
                StarView.AddDestroyableGameObject(star);
        }

        /// <summary>
        /// Clears destroyable stars from the star view.
        /// </summary>
        private void ClearStarView()
        {
            foreach (var star in StarView.GetDestroyableGameObjects())
            {
                StarView.RemoveGameObject(star);
                starStack.Push(star);
            }

            StarView.ClearDestroyableGameObjects();
        }

        #endregion

        #region Canvas Events

        private void GameCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var currentPoint = e.GetCurrentPoint(GameView);

            pointerX = currentPoint.Position.X;
        }

        #endregion

        #region Focus Events

        private void FocusBox_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Left)
            {
                moveLeft = true;
            }

            if (e.Key == Windows.System.VirtualKey.Right)
            {
                moveRight = true;
            }
        }

        private void FocusBox_KeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Left)
            {
                moveLeft = false;
            }

            if (e.Key == Windows.System.VirtualKey.Right)
            {
                moveRight = false;
            }
        }

        #endregion

        #region Window Events

        /// <summary>
        /// When the window is loaded, we add the event Current_SizeChanged.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void GamePage_Loaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SizeChanged += Current_SizeChanged;
            baseUrl = App.GetBaseUrl();
            StartGame();
        }

        /// <summary>
        /// When the window is unloaded, we remove the event Current_SizeChanged.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void GamePage_Unloaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SizeChanged -= Current_SizeChanged;
            StopGame();
        }

        /// <summary>
        /// When the window size is changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            windowWidth = Window.Current.Bounds.Width;
            windowHeight = Window.Current.Bounds.Height;

            SetCanvasSize();
            SetPlayerY();
        }

        #endregion

        #region Audio Methods

        /// <summary>
        /// Plays the background music.
        /// </summary>
        private void PlayBackgroundMusic()
        {
            var musicTrack = random.Next(1, 12);

            string host = null;

            switch (musicTrack)
            {
                case 1: { host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/slow-trap-18565.mp3"; } break;
                case 2: { host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/space-chillout-14194.mp3"; } break;
                case 3: { host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/cinematic-space-drone-10623.mp3"; } break;
                case 4: { host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/slow-thoughtful-sad-piano-114586.mp3"; } break;
                case 5: { host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/space-age-10714.mp3"; } break;
                case 6: { host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/drone-space-main-9706.mp3"; } break;
                case 7: { host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/cyberpunk-2099-10701.mp3"; } break;
                case 8: { host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/insurrection-10941.mp3"; } break;
                case 9: { host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/space-trip-114102.mp3"; } break;
                case 10: { host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/dark-matter-10710.mp3"; } break;
                case 11: { host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/music-807dfe09ce23793891674eb022b38c1b.mp3"; } break;
                default:
                    break;
            }

            if (backgroundAudio is null)
            {
                backgroundAudio = OpenSilver.Interop.ExecuteJavaScript(@"
                (function() {
                    var backgroundAudio = new Audio($0);
                    backgroundAudio.loop = true;
                    backgroundAudio.volume = 0.8;
                    return backgroundAudio;
                }())", host);
            }
            else
            {
                backgroundAudio = OpenSilver.Interop.ExecuteJavaScript(@"
                (function() {
                    $0.src = $1;
                    return $0;
                }())", backgroundAudio, host);
            }

            AudioService.PlayAudio(backgroundAudio);
        }

        /// <summary>
        /// Stops the background music.
        /// </summary>
        private void StopBackgroundMusic()
        {
            if (backgroundAudio is not null)
            {
                AudioService.PauseAudio(backgroundAudio);
            }
        }

        /// <summary>
        /// Plays the power up audio.
        /// </summary>
        private void PlayPowerUpSound()
        {
            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/spellcast-46164.mp3";

            if (powerUpAudio is null)
                powerUpAudio = OpenSilver.Interop.ExecuteJavaScript(@"
                (function() {                 
                    var powerUpAudio = new Audio($0);
                    powerUpAudio.volume = 1.0;
                   return powerUpAudio;
                }())", host);


            AudioService.PlayAudio(powerUpAudio);
        }

        /// <summary>
        /// Plays the power down audio.
        /// </summary>
        private void PlayPowerDownSound()
        {
            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/power-down-7103.mp3";

            if (powerDownAudio is null)
                powerDownAudio = OpenSilver.Interop.ExecuteJavaScript(@"
                (function() {                 
                    var powerDownAudio = new Audio($0);
                    powerDownAudio.volume = 1.0;
                   return powerDownAudio;
                }())", host);

            AudioService.PlayAudio(powerDownAudio);
        }

        #endregion

        #endregion
    }
}
