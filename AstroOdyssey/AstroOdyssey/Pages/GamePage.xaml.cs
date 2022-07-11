﻿using System;
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

        private const float FRAME_CAP_MS = 1000.0f / 60.0f;

        private int fpsCount;
        private int fpsCounter;
        private int frameStatUpdateCounter;
        private int frameStatUpdateLimit;

        private float lastFrameTime;
        private long frameStartTime;
        private long frameTime;
        private int frameDuration = 10;

        private bool gameIsRunning;

        private int enemyCounter;
        private int enemySpawnLimit;
        private double enemySpeed;

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

        private double windowWidth, windowHeight;
        private double playerX, playerWidthHalf;

        private double pointerX;

        private double laserTime;
        private double laserSpeed;
        private bool powerUpTriggered;
        private int powerUpTriggerCounter;
        private int powerUpTriggerLimit;

        private object backgroundAudio = null;
        private object laserAudio = null;
        private object enemyDestructionAudio = null;
        private object meteorDestructionAudio = null;
        private object laserImpactAudio = null;
        private object playerHealthLossAudio = null;
        private object playerHealthGainAudio = null;
        private object levelUpAudio = null;
        private object powerUpAudio = null;
        private object powerDownAudio = null;

        private Player player;
        private Rect playerBounds;

        private int playerDamagedOpacityCount;
        private int playerDamagedOpacityLimit;

        private Difficulty difficulty = Difficulty.StartUp;
        private int showInGameTextCount;
        private int showInGameTextLimit;

        private readonly Random rand = new Random();

        private readonly Stack<GameObject> enemyStack = new Stack<GameObject>();
        private readonly Stack<GameObject> meteorStack = new Stack<GameObject>();
        private readonly Stack<GameObject> healthStack = new Stack<GameObject>();
        private readonly Stack<GameObject> powerUpStack = new Stack<GameObject>();
        private readonly Stack<GameObject> starStack = new Stack<GameObject>();

        private bool moveLeft = false, moveRight = false;

        #endregion

        #region Ctor

        /// <summary>
        /// Constructor
        /// </summary>
        public GamePage()
        {
            InitializeComponent();

            Loaded += Window_SizeChanged_Demo_Loaded;
            Unloaded += Window_SizeChanged_Demo_Unloaded;

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

            GetPlayerBounds();

            SpawnEnemy();

            SpawnMeteor();

            SpawnHealth();

            SpawnPowerUp();

            SpawnStar();

            MovePlayer();

            UpdateGameView();

            UpdateStarView();

            SetDifficulty();

            HideInGameText();

            UnsetPowerUp();

            ScaleDifficulty();

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

        /// <summary>
        /// Calculates the duration of a frame.
        /// </summary>
        /// <param name="watch"></param>
        /// <returns></returns>
        private void CalculateFrameDuration(Stopwatch watch)
        {
            frameTime = watch.ElapsedMilliseconds - frameStartTime;
            frameDuration = Math.Max((int)(FRAME_CAP_MS - frameTime), 1);
        }

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
            RunLaserLoop();
        }

        /// <summary>
        /// Sets the game environment to it's default state.
        /// </summary>
        private void SetDefaultGameEnvironment()
        {
            enemyCounter = 100;
            enemySpawnLimit = 45;
            enemySpeed = 5;

            meteorCounter = 100;
            meteorSpawnLimit = 50;
            meteorSpeed = 2;

            healthCounter = 1000;
            healthSpawnLimit = 1000;
            healthSpeed = 3;

            powerUpCounter = 1000;
            powerUpSpawnLimit = 1000;
            powerUpSpeed = 3;

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

            laserTime = 235;
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
            HealthText.Text = GetPlayerHealthPoints();
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
        /// Shows the level up text in game view.
        /// </summary>
        private void ShowInGameText(string text)
        {
            InGameText.Text = text;
            InGameText.Visibility = Visibility.Visible;
            showInGameTextCount = showInGameTextLimit;
        }

        /// <summary>
        /// Hides the level up text after keeping it visible.
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

        /// <summary>
        /// Checks if a two rects intersect.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private bool IntersectsWith(Rect source, Rect target)
        {
            if (source.Width >= 0.0 && target.Width >= 0.0 && target.X <= source.X + source.Width && target.X + target.Width >= source.X && target.Y <= source.Y + source.Height)
            {
                return target.Y + target.Height >= source.Y;
            }

            return false;
        }

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
            if (gameObject.IsDestroyable)
            {
                // move enemy down
                if (gameObject is Enemy enemyElement)
                    enemyElement.MoveY();

                // move meteor down
                if (gameObject is Meteor meteorElement)
                {
                    meteorElement.Rotate();
                    meteorElement.MoveY();
                }

                // if enemy or meteor object has gone below game view
                if (gameObject.GetY() > GameView.Height)
                    GameView.AddDestroyableGameObject(gameObject);

                Rect elementBounds = gameObject.GetRect();

                // if enemy or meteor collides with player
                if (IntersectsWith(playerBounds, elementBounds))
                {
                    GameView.AddDestroyableGameObject(gameObject);

                    PlayerHealthLoss();
                }
                else
                {
                    var lasers = GameView.GetGameObjects<Laser>().Where(laser => IntersectsWith(laser.GetRect(), elementBounds));

                    if (lasers is not null && lasers.Any())
                    {
                        foreach (var laser in lasers)
                        {
                            GameView.AddDestroyableGameObject(laser);

                            // if power up is triggered then execute one shot kill
                            if (powerUpTriggered)
                                gameObject.LooseHealth(gameObject.Health);
                            else
                                gameObject.LooseHealth();

                            if (gameObject is Meteor meteor)
                            {
                                // move the meteor backwards a bit on laser hit
                                meteor.MoveY(meteor.Speed * 3 / 2, YDirection.UP);

                                PlayLaserImpactSound();

                                if (meteor.HasNoHealth)
                                {
                                    DestroyMeteor(meteor);
                                }
                            }

                            if (gameObject is Enemy enemy)
                            {
                                // move the enemy backwards a bit on laser hit
                                enemy.MoveY(enemy.Speed * 3 / 2, YDirection.UP);

                                PlayLaserImpactSound();

                                if (enemy.HasNoHealth)
                                {
                                    DestroyEnemy(enemy);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (gameObject is Laser laserElement)
                {
                    // move laser up                
                    laserElement.MoveY();

                    // remove laser if outside game canvas
                    if (laserElement.GetY() < 10)
                        GameView.AddDestroyableGameObject(laserElement);
                }

                if (gameObject is Health health)
                {
                    // move Health down
                    health.MoveY();

                    // if health object has gone below game view
                    if (health.GetY() > GameView.Height)
                        GameView.AddDestroyableGameObject(health);

                    if (IntersectsWith(playerBounds, health.GetRect()))
                    {
                        GameView.AddDestroyableGameObject(health);
                        PlayerHealthGain(health);
                    }
                }

                if (gameObject is PowerUp powerUp)
                {
                    // move PowerUp down
                    powerUp.MoveY();

                    // if PowerUp object has gone below game view
                    if (powerUp.GetY() > GameView.Height)
                        GameView.AddDestroyableGameObject(powerUp);

                    if (IntersectsWith(playerBounds, powerUp.GetRect()))
                    {
                        GameView.AddDestroyableGameObject(powerUp);
                        TriggerPowerUp();
                    }
                }
            }
        }

        /// <summary>
        /// Clears destroyable objects from game view.
        /// </summary>
        private void ClearGameView()
        {
            foreach (var destroyable in GameView.GetDestroyableGameObjects())
            {
                GameView.RemoveGameObject(destroyable);

                if (destroyable is Enemy enemy)
                    enemyStack.Push(enemy);

                if (destroyable is Meteor meteor)
                    meteorStack.Push(meteor);

                if (destroyable is Health health)
                    healthStack.Push(health);

                //if (destroyable is Laser laser)
                //    laserStack.Push(laser);

                // TODO: add storyboard animation for destruction
            }

            GameView.ClearDestroyableGameObjects();
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
        /// Sets the difficulty of the game according to score; 
        /// </summary>
        private void SetDifficulty()
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
                ShowLevelUp();
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
                        enemySpawnLimit = 45;
                        enemySpeed = 5;

                        laserTime = 235;

                        starSpeed = 0.1d;
                    }
                    break;
                case Difficulty.StartUp:
                    {
                        enemySpawnLimit = 45;
                        enemySpeed = 5;

                        laserTime = 230;

                        starSpeed = 0.1d;
                    }
                    break;
                case Difficulty.Easy:
                    {
                        enemySpawnLimit = 40;
                        enemySpeed = 7;

                        meteorSpawnLimit = 40;
                        meteorSpeed = 4;

                        laserTime = 225;

                        healthSpeed = 5;
                        powerUpSpeed = 5;

                        starSpeed = 0.2d;
                    }
                    break;
                case Difficulty.Medium:
                    {
                        enemySpawnLimit = 35;
                        enemySpeed = 9;

                        meteorSpawnLimit = 35;
                        meteorSpeed = 6;

                        laserTime = 220;

                        healthSpeed = 8;
                        powerUpSpeed = 8;

                        starSpeed = 0.3d;
                    }
                    break;
                case Difficulty.Hard:
                    {
                        enemySpawnLimit = 30;
                        enemySpeed = 11;

                        meteorSpawnLimit = 30;
                        meteorSpeed = 8;

                        laserTime = 215;

                        healthSpeed = 10;
                        powerUpSpeed = 10;

                        starSpeed = 0.4d;
                    }
                    break;
                case Difficulty.VeryHard:
                    {
                        enemySpawnLimit = 25;
                        enemySpeed = 13;

                        meteorSpawnLimit = 25;
                        meteorSpeed = 10;

                        laserTime = 210;

                        healthSpeed = 12;
                        powerUpSpeed = 12;

                        starSpeed = 0.5d;
                    }
                    break;
                case Difficulty.Extreme:
                    {
                        enemySpawnLimit = 20;
                        enemySpeed = 15;

                        meteorSpawnLimit = 20;
                        meteorSpeed = 12;

                        laserTime = 205;

                        healthSpeed = 14;
                        powerUpSpeed = 14;

                        starSpeed = 0.6d;
                    }
                    break;
                case Difficulty.Pro:
                    {
                        enemySpawnLimit = 15;
                        enemySpeed = 17;

                        meteorSpawnLimit = 15;
                        meteorSpeed = 14;

                        laserTime = 200;

                        healthSpeed = 16;
                        powerUpSpeed = 16;

                        starSpeed = 0.7d;
                    }
                    break;
                default:
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
            player.AddToGameEnvironment(windowHeight - player.Height - 20, pointerX, GameView);
        }

        /// <summary>
        /// Gets the players x axis position and bounds.
        /// </summary>
        private void GetPlayerBounds()
        {
            playerWidthHalf = player.Width / 2;

            playerX = player.GetX();
            playerBounds = player.GetRect();
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
        /// Checks if there is any game object within the left side range of the player
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        private bool AnyObjectWithinPlayersLeftSideRange(GameObject go)
        {
            var left = go.GetX();

            return left + go.Width / 2 < playerX && left + go.Width / 2 > playerX - 250;
        }

        /// <summary>
        /// Checks if there is any game object within the right side range of the player
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        private bool AnyObjectWithinPlayersRightRange(GameObject go)
        {
            var left = go.GetX();

            return left + go.Width / 2 > playerX && left + go.Width / 2 <= playerX + 250;
        }

        /// <summary>
        /// Moves the player to last pointer pressed position by x axis.
        /// </summary>
        private void MovePlayer()
        {
            if (moveLeft && playerX > 0)
                pointerX -= playerSpeed;

            if (moveRight && playerX + player.Width < windowWidth)
                pointerX += playerSpeed;

            // move right
            if (pointerX - playerWidthHalf > playerX + playerSpeed)
            {
                if (playerX + playerWidthHalf < windowWidth)
                {
                    SetPlayerX(playerX + playerSpeed);
                }
            }

            // move left
            if (pointerX - playerWidthHalf < playerX - playerSpeed)
            {
                SetPlayerX(playerX - playerSpeed);
            }
        }

        /// <summary>
        /// Gets the player health points.
        /// </summary>
        /// <returns></returns>
        private string GetPlayerHealthPoints()
        {
            var healthPoints = player.Health / player.HealthSlot;
            var healthIcon = "❤️";
            var health = string.Empty;

            for (int i = 0; i < healthPoints; i++)
            {
                health += healthIcon;
            }

            return health;
        }

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

        #endregion

        #region Laser Methods

        /// <summary>
        /// Runs the laser loop if game is running.
        /// </summary>
        private async void RunLaserLoop()
        {
            while (gameIsRunning)
            {
                // any object falls within player range
                if (GameView.GetGameObjects<GameObject>().Where(x => x.IsDestroyable).Any(x => AnyObjectWithinPlayersRightRange(x) || AnyObjectWithinPlayersLeftSideRange(x)))
                {
                    SpawnLaser();
                    PlayLaserSound();
                }

                await Task.Delay(TimeSpan.FromMilliseconds(laserTime));
            }
        }

        /// <summary>
        /// Spawns a laser.
        /// </summary>
        private void SpawnLaser()
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

            GenerateLaser(laserHeight, laserWidth);
        }

        /// <summary>
        /// Generates a laser.
        /// </summary>
        /// <param name="laserHeight"></param>
        /// <param name="laserWidth"></param>
        private void GenerateLaser(double laserHeight, double laserWidth)
        {
            var newLaser = /*laserStack.Count() > 10 ? laserStack.Pop() as Laser :*/ new Laser();

            newLaser.SetAttributes(speed: laserSpeed, height: laserHeight, width: laserWidth, isPoweredUp: powerUpTriggered);

            newLaser.AddToGameEnvironment(top: player.GetY() - 20, left: player.GetX() + player.Width / 2 - newLaser.Width / 2, gameEnvironment: GameView);
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
                enemyCounter = enemySpawnLimit;
            }
        }

        /// <summary>
        /// Generates a random enemy.
        /// </summary>
        private void GenerateEnemy()
        {
            var newEnemy = enemyStack.Any() ? enemyStack.Pop() as Enemy : new Enemy();

            newEnemy.SetAttributes(enemySpeed + rand.Next(0, 4));
            newEnemy.AddToGameEnvironment(-100, rand.Next(10, (int)windowWidth - 100), GameView);
        }

        /// <summary>
        /// Destroys an enemy. Removes from game environment, increases player score, plays sound effect.
        /// </summary>
        /// <param name="meteor"></param>
        private void DestroyEnemy(Enemy enemy)
        {
            GameView.AddDestroyableGameObject(enemy);

            PlayerScoreByEnemyDestruction();

            PlayEnemyDestructionSound();
        }

        #endregion

        #region Meteor Methods

        /// <summary>
        /// Spawns a meteor.
        /// </summary>
        private void SpawnMeteor()
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

        /// <summary>
        /// Generates a random meteor.
        /// </summary>
        private void GenerateMeteor()
        {
            var newMeteor = meteorStack.Any() ? meteorStack.Pop() as Meteor : new Meteor();

            newMeteor.SetAttributes(meteorSpeed + rand.NextDouble());
            newMeteor.AddToGameEnvironment(top: -100, left: rand.Next(10, (int)windowWidth - 100), gameEnvironment: GameView);
        }

        /// <summary>
        /// Destroys a meteor. Removes from game environment, increases player score, plays sound effect.
        /// </summary>
        /// <param name="meteor"></param>
        private void DestroyMeteor(Meteor meteor)
        {
            GameView.AddDestroyableGameObject(meteor);

            PlayerScoreByMeteorDestruction();

            PlayMeteorDestructionSound();
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

            newHealth.SetAttributes(healthSpeed + rand.NextDouble());
            newHealth.AddToGameEnvironment(top: -100, left: rand.Next(10, (int)windowWidth - 100), gameEnvironment: GameView);
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

            newPowerUp.SetAttributes(powerUpSpeed + rand.NextDouble());
            newPowerUp.AddToGameEnvironment(top: -100, left: rand.Next(10, (int)windowWidth - 100), gameEnvironment: GameView);
        }

        /// <summary>
        /// Triggers the powered up state.
        /// </summary>
        private void TriggerPowerUp()
        {
            powerUpTriggered = true;
            powerUpTriggerCounter = powerUpTriggerLimit;
            ShowInGameText("POWER UP!");
            PlayPowerUpSound();
            player.SetPowerUp();
        }

        /// <summary>
        /// Unsets the power up trigger after keeping it active for 500 frames.
        /// </summary>
        private void UnsetPowerUp()
        {
            if (powerUpTriggered)
            {
                powerUpTriggerCounter -= 1;

                if (powerUpTriggerCounter <= 0)
                {
                    powerUpTriggered = false;
                    PlayPowerDownSound();
                    player.SetPowerDown();
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
            newStar.AddToGameEnvironment(top: 0 - newStar.Height, left: rand.Next(10, (int)windowWidth - 10), gameEnvironment: StarView);
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
        void Window_SizeChanged_Demo_Loaded(object sender, RoutedEventArgs e)
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
        void Window_SizeChanged_Demo_Unloaded(object sender, RoutedEventArgs e)
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
            var musicTrack = rand.Next(1, 12);

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

            PlayAudio(backgroundAudio);
        }

        /// <summary>
        /// Stops the background music.
        /// </summary>
        private void StopBackgroundMusic()
        {
            if (backgroundAudio is not null)
            {
                PauseAudio(backgroundAudio);
            }
        }

        /// <summary>
        /// Plays the laser sound efect.
        /// </summary>
        private void PlayLaserSound()
        {
            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/beam-8-43831.mp3";

            if (laserAudio is null)
            {
                laserAudio = OpenSilver.Interop.ExecuteJavaScript(@"
                (function() {
                    //play audio with out html audio tag
                    var laserAudio = new Audio($0);
                    laserAudio.volume = 0.1;
                    return laserAudio;
                }())", host);
            }

            PlayAudio(laserAudio);
        }

        /// <summary>
        /// Plays the enemy destruction sound effect.
        /// </summary>
        private void PlayEnemyDestructionSound()
        {
            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/explosion-36210.mp3";

            if (enemyDestructionAudio is null)
            {
                enemyDestructionAudio = OpenSilver.Interop.ExecuteJavaScript(@"
                (function() {
                    //play audio with out html audio tag
                    var enemyDestructionAudio = new Audio($0);
                    enemyDestructionAudio.volume = 0.8;                
                    return enemyDestructionAudio;
                }())", host);
            }

            PlayAudio(enemyDestructionAudio);
        }

        /// <summary>
        /// Plays the sound effect when a laser hits a meteor or an enemy.
        /// </summary>
        private void PlayLaserImpactSound()
        {
            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/explosion-sfx-43814.mp3";

            if (laserImpactAudio is null)
            {
                laserImpactAudio = OpenSilver.Interop.ExecuteJavaScript(@"
                (function() {
                    //play audio with out html audio tag
                    var laserImpactAudio = new Audio($0);
                    laserImpactAudio.volume = 0.6;
                    return laserImpactAudio;
                }())", host);
            }

            PlayAudio(laserImpactAudio);
        }

        /// <summary>
        /// Plays the meteor destruction sound effect.
        /// </summary>
        private void PlayMeteorDestructionSound()
        {
            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/explosion-36210.mp3";

            if (meteorDestructionAudio is null)
            {
                meteorDestructionAudio = OpenSilver.Interop.ExecuteJavaScript(@"
                (function() {
                    //play audio with out html audio tag
                    var meteorDestructionAudio = new Audio($0);
                    meteorDestructionAudio.volume = 0.8;
                    return meteorDestructionAudio;
                }())", host);
            }

            PlayAudio(meteorDestructionAudio);
        }

        /// <summary>
        /// Plays the sound effect when the player looses health.
        /// </summary>
        private void PlayPlayerHealthLossSound()
        {
            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/explosion-39897.mp3";

            if (playerHealthLossAudio is null)
            {
                playerHealthLossAudio = OpenSilver.Interop.ExecuteJavaScript(@"
                (function() {
                    //play audio with out html audio tag
                    var playerHealthDecreaseAudio = new Audio($0);
                    playerHealthDecreaseAudio.volume = 1.0;
                   return playerHealthDecreaseAudio;
                }())", host);
            }

            PlayAudio(playerHealthLossAudio);
        }

        /// <summary>
        /// Plays the sound effect when the player gains health.
        /// </summary>
        private void PlayPlayerHealthGainSound()
        {
            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/scale-e6-14577.mp3";

            if (playerHealthGainAudio is null)
            {
                playerHealthGainAudio = OpenSilver.Interop.ExecuteJavaScript(@"
                (function() {
                    //play audio with out html audio tag
                    var playerHealthGainAudio = new Audio($0);
                    playerHealthGainAudio.volume = 1.0;
                   return playerHealthGainAudio;
                }())", host);
            }

            PlayAudio(playerHealthGainAudio);
        }

        /// <summary>
        /// Plays the level up audio.
        /// </summary>
        private void PlayLevelUpSound()
        {
            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/8-bit-powerup-6768.mp3";

            if (levelUpAudio is null)
            {
                levelUpAudio = OpenSilver.Interop.ExecuteJavaScript(@"
                (function() {
                    //play audio with out html audio tag
                    var levelUpAudio = new Audio($0);
                    levelUpAudio.volume = 1.0;
                   return levelUpAudio;
                }())", host);
            }

            PlayAudio(levelUpAudio);
        }

        /// <summary>
        /// Plays the power up audio.
        /// </summary>
        private void PlayPowerUpSound()
        {
            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/spellcast-46164.mp3";

            if (powerUpAudio is null)
            {
                powerUpAudio = OpenSilver.Interop.ExecuteJavaScript(@"
                (function() {                 
                    var powerUpAudio = new Audio($0);
                    powerUpAudio.volume = 1.0;
                   return powerUpAudio;
                }())", host);
            }

            PlayAudio(powerUpAudio);
        }

        /// <summary>
        /// Plays the power down audio.
        /// </summary>
        private void PlayPowerDownSound()
        {
            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/power-down-7103.mp3";

            if (powerDownAudio is null)
            {
                powerDownAudio = OpenSilver.Interop.ExecuteJavaScript(@"
                (function() {                 
                    var powerDownAudio = new Audio($0);
                    powerDownAudio.volume = 1.0;
                   return powerDownAudio;
                }())", host);
            }

            PlayAudio(powerDownAudio);
        }

        /// <summary>
        /// Plays the provided js audio object.
        /// </summary>
        /// <param name="audio"></param>
        private void PlayAudio(object audio)
        {
            OpenSilver.Interop.ExecuteJavaScript(@"
            (function() { 
                //play audio with out html audio tag
                $0.currentTime =0;
                $0.play();           
            }())", audio);
        }

        /// <summary>
        /// Pauses the provided js audio object.
        /// </summary>
        /// <param name="audio"></param>
        private void PauseAudio(object audio)
        {
            OpenSilver.Interop.ExecuteJavaScript(@"
            (function() { 
                //play audio with out html audio tag              
                $0.pause();           
            }())", audio);
        }

        #endregion

        #endregion
    }
}
