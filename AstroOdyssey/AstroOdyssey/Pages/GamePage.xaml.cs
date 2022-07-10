using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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

        private object backgroundAudio = null;
        private object laserAudio = null;
        private object enemyDestructionAudio = null;
        private object meteorDestructionAudio = null;
        private object laserHitObjectAudio = null;
        private object playerHealthLossAudio = null;
        private object playerHealthGainAudio = null;
        private object levelUpAudio = null;

        private Player player;
        private Rect playerBounds;

        private int playerDamagedOpacityCount;
        private int playerDamagedOpacityLimit;

        private Difficulty difficulty = Difficulty.StartUp;
        private int showLevelUpCount;
        private int showLevelUpLimit;

        private readonly Random rand = new Random();

        private readonly List<GameObject> destroyableGameCanvasObjects = new List<GameObject>();
        private readonly List<GameObject> destroyableStarCanvasObjects = new List<GameObject>();

        //private readonly Stack<Laser> laserStack = new Stack<Laser>();
        private readonly Stack<Enemy> enemyStack = new Stack<Enemy>();
        private readonly Stack<Meteor> meteorStack = new Stack<Meteor>();
        private readonly Stack<Health> healthStack = new Stack<Health>();
        //private readonly Stack<Star> starStack = new Stack<Star>();

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

            difficulty = Difficulty.Noob;
            showLevelUpLimit = 100;

            GameCanvas.Children.Clear();
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

                UpdateGameStats();

                UpdateFrameStats();

                GetPlayerBounds();

                SpawnEnemy();

                SpawnMeteor();

                SpawnHealth();

                SpawnStar();

                MovePlayer();

                UpdateFrame();

                UpdateStars();

                SetDifficulty();

                HideLevelUp();

                ScaleDifficulty();

                PlayerOpacity();

                CheckPlayerDeath();

                CalculateFps();

                FocusBox.Focus();

                await ElapseFrameDuration(watch);
            }
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

            if (currentDifficulty != difficulty)
            {
                LevelUpText.Visibility = Visibility.Visible;
                showLevelUpCount = showLevelUpLimit;
                PlayLevelUpSound();
            }
        }

        /// <summary>
        /// Hides the level up text after keeping it visible for 100 frames.
        /// </summary>
        private void HideLevelUp()
        {
            showLevelUpCount -= 1;

            if (showLevelUpCount <= 0)
            {
                LevelUpText.Visibility = Visibility.Collapsed;
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

                        starSpeed = 0.7d;
                    }
                    break;
                default:
                    break;
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
            GameCanvas.Height = windowHeight;
            GameCanvas.Width = windowWidth;

            StarCanvas.Height = windowHeight;
            StarCanvas.Width = windowWidth;
        }

        #endregion

        #region Frame Methods

        /// <summary>
        /// Updates a frame. Advances game objects in the frame.
        /// </summary>
        private void UpdateFrame()
        {
            var gameObjects = GameCanvas.Children.OfType<GameObject>().Where(x => x is not Player);

            foreach (var gameObject in gameObjects)
            {
                UpdateLaserElement(gameObject);
                UpdateDestroyableElement(gameObject);
                UpdateHealthElement(gameObject);
            }

            foreach (var destroyable in destroyableGameCanvasObjects)
            {
                GameCanvas.Children.Remove(destroyable);

                //if (destroyable is Laser laser)
                //    laserStack.Push(laser);
                if (destroyable is Enemy enemy)
                    enemyStack.Push(enemy);
                if (destroyable is Meteor meteor)
                    meteorStack.Push(meteor);
                if (destroyable is Health health)
                    healthStack.Push(health);

                // TODO: add storyboard animation for destruction
            }

            destroyableGameCanvasObjects.Clear();
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
                FrameDurationText.Text = "Frame duration: " + frameDuration + "ms";
                ObjectsCountText.Text = "Objects count: " + GameCanvas.Children.Count();

                frameStatUpdateCounter = frameStatUpdateLimit;
            }
        }

        /// <summary>
        /// Update a destroyable element. Finds intersecting lasers and performs destruction.
        /// </summary>
        /// <param name="element"></param>
        private void UpdateDestroyableElement(GameObject element)
        {
            if (element.IsDestroyable)
            {
                if (element is Enemy enemyElement)
                {
                    // move enemy down
                    Canvas.SetTop(enemyElement, Canvas.GetTop(enemyElement) + enemySpeed);
                }

                if (element is Meteor meteorElement)
                {
                    // move meteor down
                    Canvas.SetTop(meteorElement, Canvas.GetTop(meteorElement) + meteorSpeed);
                }

                Rect elementBounds = element.GetRect();

                if (IntersectsWith(playerBounds, elementBounds))
                {
                    destroyableGameCanvasObjects.Add(element);

                    PlayerHealthLoss();
                }
                else
                {
                    if (Canvas.GetTop(element) > windowHeight)
                    {
                        destroyableGameCanvasObjects.Add(element);
                    }
                    else
                    {
                        var lasers = GameCanvas.Children.OfType<Laser>().Where(laser => IntersectsWith(laser.GetRect(), elementBounds));

                        if (lasers is not null && lasers.Any())
                        {
                            foreach (var laser in lasers)
                            {
                                destroyableGameCanvasObjects.Add(laser);

                                element.LooseHealth();

                                if (element is Meteor meteor)
                                {
                                    // move the meteor backwards a bit on laser hit
                                    Canvas.SetTop(meteor, Canvas.GetTop(meteor) - (meteorSpeed * 3) / 2);

                                    PlayLaserHitObjectSound();

                                    if (meteor.HasNoHealth)
                                    {
                                        destroyableGameCanvasObjects.Add(meteor);

                                        PlayerScoreByMeteorDestruction();

                                        PlayMeteorDestructionSound();
                                    }
                                }

                                if (element is Enemy enemy)
                                {
                                    // move the enemy backwards a bit on laser hit
                                    Canvas.SetTop(enemy, Canvas.GetTop(enemy) - (enemySpeed * 3) / 2);

                                    PlayLaserHitObjectSound();

                                    if (enemy.HasNoHealth)
                                    {
                                        destroyableGameCanvasObjects.Add(enemy);

                                        PlayerScoreByEnemyDestruction();

                                        PlayEnemyDestructionSound();
                                    }
                                }
                            }
                        }
                    }
                }
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

        #region Player Methods

        /// <summary>
        /// Spawns the player.
        /// </summary>
        private void SpawnPlayer()
        {
            player = new Player();

            Canvas.SetLeft(player, pointerX);
            SetPlayerCanvasTop();

            GameCanvas.Children.Add(player);
        }

        /// <summary>
        /// Gets the players x axis position and bounds.
        /// </summary>
        private void GetPlayerBounds()
        {
            playerX = Canvas.GetLeft(player);
            playerWidthHalf = player.Width / 2;

            playerBounds = player.GetRect();
        }

        /// <summary>
        /// Sets the y axis position of the player on game canvas.
        /// </summary>
        private void SetPlayerCanvasTop()
        {
            Canvas.SetTop(player, windowHeight - player.Height - 20);
        }

        /// <summary>
        /// Sets the x axis position of the player on game canvas.
        /// </summary>
        /// <param name="x"></param>
        private void SetPlayerCanvasLeft(double x)
        {
            Canvas.SetLeft(player, x);
        }

        /// <summary>
        /// Checks if there is any game object within the left side range of the player
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        private bool AnyObjectWithinPlayersLeftSideRange(GameObject go)
        {
            return (Canvas.GetLeft(go) + go.Width / 2 < playerX && Canvas.GetLeft(go) + go.Width / 2 > playerX - 250);
        }

        /// <summary>
        /// Checks if there is any game object within the right side range of the player
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        private bool AnyObjectWithinPlayersRightRange(GameObject go)
        {
            return (Canvas.GetLeft(go) + go.Width / 2 > playerX && Canvas.GetLeft(go) + go.Width / 2 <= playerX + 250);
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
                    SetPlayerCanvasLeft(playerX + playerSpeed);
                }
            }

            // move left
            if (pointerX - playerWidthHalf < playerX - playerSpeed)
            {
                SetPlayerCanvasLeft(playerX - playerSpeed);
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

                var contentDialogue = new MessageDialogueWindow(title: "Game Over!", message: "Would you like to play again?", result: (result) =>
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
            var newHealth = healthStack.Any() ? healthStack.Pop() : new Health();

            newHealth.SetAttributes();

            Canvas.SetTop(newHealth, -100);
            Canvas.SetLeft(newHealth, rand.Next(10, (int)windowWidth - 100));
            GameCanvas.Children.Add(newHealth);
        }

        /// <summary>
        /// Update the health element as per frame.
        /// </summary>
        /// <param name="element"></param>
        private void UpdateHealthElement(GameObject element)
        {
            if (element is Health health)
            {
                // move Health down
                Canvas.SetTop(health, Canvas.GetTop(health) + healthSpeed);

                Rect healthHitBox = health.GetRect();

                if (IntersectsWith(playerBounds, healthHitBox))
                {
                    destroyableGameCanvasObjects.Add(health);

                    PlayerHealthGain(health);
                }
                else
                {
                    if (Canvas.GetTop(health) > windowHeight)
                    {
                        destroyableGameCanvasObjects.Add(health);
                    }
                }
            }
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
                if (GameCanvas.Children.OfType<GameObject>().Where(x => x.IsDestroyable).Any(x => AnyObjectWithinPlayersRightRange(x) || AnyObjectWithinPlayersLeftSideRange(x)))
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
            var newLaser = /*laserStack.Any() ? laserStack.Pop() :*/ new Laser();

            newLaser.SetAttributes(laserHeight, laserWidth);

            Canvas.SetLeft(newLaser, Canvas.GetLeft(player) + player.Width / 2 - newLaser.Width / 2);
            Canvas.SetTop(newLaser, Canvas.GetTop(player) - 20);

            GameCanvas.Children.Add(newLaser);
        }

        /// <summary>
        /// Update the laser element as per frame.
        /// </summary>
        /// <param name="element"></param>
        private void UpdateLaserElement(GameObject element)
        {
            if (element is Laser laser)
            {
                // move laser up
                Canvas.SetTop(laser, Canvas.GetTop(laser) - laserSpeed);

                // remove laser if outside game canvas
                if (Canvas.GetTop(laser) < 10)
                {
                    destroyableGameCanvasObjects.Add(laser);
                }
            }
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
            var newEnemy = enemyStack.Any() ? enemyStack.Pop() : new Enemy();

            newEnemy.SetAttributes();

            Canvas.SetTop(newEnemy, -100);
            Canvas.SetLeft(newEnemy, rand.Next(10, (int)windowWidth - 100));
            GameCanvas.Children.Add(newEnemy);
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
            var newMeteor = meteorStack.Any() ? meteorStack.Pop() : new Meteor();

            newMeteor.SetAttributes();

            Canvas.SetTop(newMeteor, -100);
            Canvas.SetLeft(newMeteor, rand.Next(10, (int)windowWidth - 100));
            GameCanvas.Children.Add(newMeteor);
        }

        #endregion

        #region Star Methods

        /// <summary>
        /// Spawns random stars in the star canvas.
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
            var newStar = /*starStack.Any() ? starStack.Pop() :*/ new Star();

            newStar.SetAttributes();

            Canvas.SetTop(newStar, -100);
            Canvas.SetLeft(newStar, rand.Next(10, (int)windowWidth - 10));
            StarCanvas.Children.Add(newStar);
        }

        /// <summary>
        /// Updates stars on the star canvas.
        /// </summary>
        private void UpdateStars()
        {
            var starObjects = StarCanvas.Children.OfType<GameObject>();

            foreach (var star in starObjects)
            {
                UpdateStarElement(star);
            }

            foreach (var destroyable in destroyableStarCanvasObjects)
            {
                //if (destroyable is Star star)
                //    starStack.Push(star);

                StarCanvas.Children.Remove(destroyable);
            }

            destroyableStarCanvasObjects.Clear();
        }

        /// <summary>
        /// Updates the star element.
        /// </summary>
        /// <param name="element"></param>
        private void UpdateStarElement(GameObject element)
        {
            if (element is Star star)
            {
                // move star down
                Canvas.SetTop(star, Canvas.GetTop(star) + starSpeed);

                if (Canvas.GetTop(star) > windowHeight)
                {
                    destroyableStarCanvasObjects.Add(star);
                }
            }
        }

        #endregion        

        #region Canvas Events

        private void GameCanvas_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var currentPoint = e.GetCurrentPoint(GameCanvas);

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
            SetPlayerCanvasTop();
        }

        #endregion

        #region Audio Methods

        /// <summary>
        /// Plays the background music.
        /// </summary>
        private void PlayBackgroundMusic()
        {
            var musicTrack = rand.Next(1, 6);

            string host = null;

            switch (musicTrack)
            {
                case 1: { host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/slow-trap-18565.mp3"; } break;
                case 2: { host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/space-chillout-14194.mp3"; } break;
                case 3: { host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/cinematic-space-drone-10623.mp3"; } break;
                case 4: { host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/slow-thoughtful-sad-piano-114586.mp3"; } break;
                case 5: { host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/space-age-10714.mp3"; } break;
                default:
                    break;
            }

            //if (backgroundAudio is null)
            //{
            backgroundAudio = OpenSilver.Interop.ExecuteJavaScript(@"
                (function() {
                    var backgroundAudio = new Audio($0);
                    backgroundAudio.loop = true;
                    return backgroundAudio;
                }())", host);
            //}
            //else
            //{
            //    backgroundAudio = OpenSilver.Interop.ExecuteJavaScript(@"
            //    (function() {
            //        $0.src = $1;
            //        return $0;
            //    }())", backgroundAudio, host);
            //}

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
        private void PlayLaserHitObjectSound()
        {
            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/explosion-sfx-43814.mp3";

            if (laserHitObjectAudio is null)
            {
                laserHitObjectAudio = OpenSilver.Interop.ExecuteJavaScript(@"
                (function() {
                    //play audio with out html audio tag
                    var laserHitObjectAudio = new Audio($0);
                    laserHitObjectAudio.volume = 0.6;
                    return laserHitObjectAudio;
                }())", host);
            }

            PlayAudio(laserHitObjectAudio);
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
