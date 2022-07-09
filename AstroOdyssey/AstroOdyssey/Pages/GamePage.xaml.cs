using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Browser;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace AstroOdyssey
{
    public partial class GamePage : Page
    {
        #region Fields

        private const float FRAME_CAP_MS = 1000.0f / 60.0f;
        private int fpsCount;
        private int fpsCounter;
        private float lastFPSTime;

        private bool isGameRunning;

        private int enemyCounter;
        private int enemySpawnWait;
        private int enemySpeed;

        private int meteorCounter;
        private int meteorSpawnWait;
        private int meteorSpeed;

        private int playerSpeed;

        private double score;

        private double windowWidth, windowHeight;
        private double playerX, playerWidthHalf;

        private double pointerX;

        private double laserTime;
        private double laserSpeed;

        private string baseUrl;

        private object backgroundAudio = null;
        private object laserAudio = null;
        private object enemyDestructionAudio = null;
        private object meteorDestructionAudio = null;
        private object laserHitObjectAudio = null;
        private object playerHealthDecreaseAudio = null;

        private Player player;
        private Rect playerBounds;

        private Difficulty difficulty = Difficulty.StartUp;

        private Random rand = new Random();
        private List<GameObject> destroyableGameObjects = new List<GameObject>();

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

            SetWindowSizeAtStartup();
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

            isGameRunning = true;

            RunGameLoop();
            RunLaserLoop();
        }

        /// <summary>
        /// Sets the game environment to it's default state.
        /// </summary>
        private void SetDefaultGameEnvironment()
        {
            enemyCounter = 100;
            enemySpawnWait = 45;
            enemySpeed = 5;

            meteorCounter = 100;
            meteorSpawnWait = 50;
            meteorSpeed = 2;

            playerSpeed = 15;

            score = 0;

            fpsCount = 0;
            fpsCounter = 0;
            lastFPSTime = 0;

            laserTime = 250;
            laserSpeed = 20;

            difficulty = Difficulty.Noob;

            GameCanvas.Children.Clear();
        }

        /// <summary>
        /// Stops the game.
        /// </summary>
        private void StopGame()
        {
            StopBackgroundMusic();
            isGameRunning = false;

            //TODO: show score  
            //TODO: ask if want to play again
            App.NavigateToPage("/GameStartPage");

        }

        /// <summary>
        /// Runs the game loop if game is running. Updates stats, gets player bounds, spawns enemies and meteors, moves the player, updates the frame, scales difficulty, checks player health, calculates fps and frame time.
        /// </summary>
        private async void RunGameLoop()
        {
            var watch = Stopwatch.StartNew();

            while (isGameRunning)
            {
                var frameStartTime = watch.ElapsedMilliseconds;

                UpdateStats();

                GetPlayerBounds();

                SpawnEnemy();

                SpawnMeteor();

                MovePlayer();

                UpdateFrame();

                SetDifficulty();

                ScaleDifficulty();

                CheckPlayerDeath();

                CalculateFps(frameStartTime);

                FocusBox.Focus();

                await AwaitFrameTime(watch, frameStartTime);
            }
        }

        /// <summary>
        /// Awaits the calculated frame time.
        /// </summary>
        /// <param name="watch"></param>
        /// <param name="frameStartTime"></param>
        /// <returns></returns>
        private async Task AwaitFrameTime(Stopwatch watch, long frameStartTime)
        {
            int frameTime = CalculateFrameTime(watch, frameStartTime);

            await Task.Delay(frameTime);
        }

        /// <summary>
        /// Runs the laser loop if game is running.
        /// </summary>
        private async void RunLaserLoop()
        {
            while (isGameRunning)
            {
                // any object falls within player range
                if (GameCanvas.Children.OfType<GameObject>().Where(x => x is Meteor || x is Enemy).Any(x => IsAnyObjectWihinRightSideRange(x) || IsAnyObjectWithinLeftSideRange(x)))
                {
                    SpawnLaser();

                    PlayLaserSound();
                }

                await Task.Delay(TimeSpan.FromMilliseconds(laserTime));
            }
        }

        /// <summary>
        /// Checks if there is any game object within the left side range of the player
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        private bool IsAnyObjectWithinLeftSideRange(GameObject go)
        {
            return (Canvas.GetLeft(go) + go.Width / 2 < playerX && Canvas.GetLeft(go) + go.Width / 2 > playerX - 250);
        }

        /// <summary>
        /// Checks if there is any game object within the right side range of the player
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        private bool IsAnyObjectWihinRightSideRange(GameObject go)
        {
            return (Canvas.GetLeft(go) + go.Width / 2 > playerX && Canvas.GetLeft(go) + go.Width / 2 <= playerX + 250);
        }

        /// <summary>
        /// Calculates the frame time.
        /// </summary>
        /// <param name="watch"></param>
        /// <param name="frameStartTime"></param>
        /// <returns></returns>
        private int CalculateFrameTime(Stopwatch watch, long frameStartTime)
        {
            var frameTime = watch.ElapsedMilliseconds - frameStartTime;
            var waitTime = Math.Max((int)(FRAME_CAP_MS - frameTime), 1);
            return waitTime;
        }

        /// <summary>
        /// Calculates frames per second.
        /// </summary>
        /// <param name="frameStartTime"></param>
        private void CalculateFps(long frameStartTime)
        {
            // calculate FPS
            if (lastFPSTime + 1000 < frameStartTime)
            {
                fpsCount = fpsCounter;
                fpsCounter = 0;
                lastFPSTime = frameStartTime;
            }

            fpsCounter++;
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
        /// Updates a frame. Advances game objects in the frame.
        /// </summary>
        private void UpdateFrame()
        {
            var gameObjects = GameCanvas.Children.OfType<GameObject>().Where(x => x is not Player);

            Parallel.ForEach(gameObjects, (element) =>
            {
                UpdateLaserElement(element);
                UpdateEnemyElement(element);
                UpdateMeteorElement(element);
            });

            Parallel.ForEach(destroyableGameObjects, (removableItem) =>
            {
                // TODO: add storyboard animation for destruction
                GameCanvas.Children.Remove(removableItem);
            });
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
                    destroyableGameObjects.Add(laser);
                }

                Rect laserBounds = laser.GetRect();

                // get game objects which are not laser
                var obstacles = GameCanvas.Children.OfType<GameObject>().Where(x => x is not Laser);

                Parallel.ForEach(obstacles, (obstacle) =>
                {
                    // check if enemy intersects the laser
                    if (obstacle is Enemy targetEnemy)
                    {
                        Rect enemyBounds = targetEnemy.GetRect();

                        if (IntersectsWith(laserBounds, enemyBounds))
                        {
                            destroyableGameObjects.Add(laser);

                            targetEnemy.LooseHealth();

                            // move the enemy backwards a bit on laser hit
                            Canvas.SetTop(targetEnemy, Canvas.GetTop(targetEnemy) - (enemySpeed * 3) / 2);

                            PlayLaserHitObjectSound();

                            if (targetEnemy.IsDestroyable)
                            {
                                destroyableGameObjects.Add(targetEnemy);

                                PlayerScoreByEnemyDestruction();

                                PlayEnemyDestructionSound();
                            }
                        }
                    }

                    // check if meteor intersects the laser
                    if (obstacle is Meteor targetMeteor)
                    {
                        Rect meteorBounds = targetMeteor.GetRect();

                        if (IntersectsWith(laserBounds, meteorBounds))
                        {
                            destroyableGameObjects.Add(laser);

                            targetMeteor.LooseHealth();

                            // move the meteor backwards a bit on laser hit
                            Canvas.SetTop(targetMeteor, Canvas.GetTop(targetMeteor) - (meteorSpeed * 4) / 2);

                            PlayLaserHitObjectSound();

                            if (targetMeteor.IsDestroyable)
                            {
                                destroyableGameObjects.Add(targetMeteor);

                                PlayerScoreByMeteorDestruction();

                                PlayMeteorDestructionSound();
                            }
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Update the enemey element as per frame.
        /// </summary>
        /// <param name="element"></param>
        private void UpdateEnemyElement(GameObject element)
        {
            if (element is Enemy enemy)
            {
                // move enemy down
                Canvas.SetTop(enemy, Canvas.GetTop(enemy) + enemySpeed);

                Rect enemyHitBox = enemy.GetRect();

                if (IntersectsWith(playerBounds, enemyHitBox))
                {
                    destroyableGameObjects.Add(enemy);

                    player.LooseHealth();

                    PlayPlayerHealthLossSound();
                }
                else
                {
                    if (Canvas.GetTop(enemy) > windowHeight)
                    {
                        destroyableGameObjects.Add(enemy);
                    }
                }
            }
        }

        /// <summary>
        /// Update the meteor element as per frame.
        /// </summary>
        /// <param name="element"></param>
        private void UpdateMeteorElement(GameObject element)
        {
            if (element is Meteor meteor)
            {
                // move meteor down
                Canvas.SetTop(meteor, Canvas.GetTop(meteor) + meteorSpeed);

                Rect meteorHitBox = meteor.GetRect();

                if (IntersectsWith(playerBounds, meteorHitBox))
                {
                    destroyableGameObjects.Add(meteor);

                    player.LooseHealth();

                    PlayPlayerHealthLossSound();
                }
                else
                {
                    if (Canvas.GetTop(meteor) > windowHeight)
                    {
                        destroyableGameObjects.Add(meteor);
                    }
                }
            }
        }

        /// <summary>
        /// Increase player score if an enemy was destroyed.
        /// </summary>
        private void PlayerScoreByEnemyDestruction()
        {
            score++;
        }

        /// <summary>
        /// Increase player score if a meteor was destroyed.
        /// </summary>
        private void PlayerScoreByMeteorDestruction()
        {
            score += 0.5d;
        }

        /// <summary>
        /// Updates the game score, player health, fps, and objects currently in view.
        /// </summary>
        private void UpdateStats()
        {
            ScoreText.Text = "Score: " + score;
            HealthText.Text =/* "Health: " +*/ GetPlayerHealthPoints();
            FPSText.Text = "FPS: " + fpsCount;
            ObjectsText.Text = "Objects: " + GameCanvas.Children.Count();
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
            if (player.IsDestroyable)
            {
                HealthText.Text = "Health: " + 0;
                StopGame();
            }
        }

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
                enemyCounter = enemySpawnWait;
            }
        }

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
                meteorCounter = meteorSpawnWait;
            }
        }

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

            var newLaser = new Laser(laserHeight, laserWidth);

            Canvas.SetLeft(newLaser, Canvas.GetLeft(player) + player.Width / 2 - newLaser.Width / 2);
            Canvas.SetTop(newLaser, Canvas.GetTop(player) - 20);

            GameCanvas.Children.Add(newLaser);
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
        /// Sets the difficulty of the game according to score; 
        /// </summary>
        private void SetDifficulty()
        {
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
                        enemySpawnWait = 45;
                        enemySpeed = 5;
                        laserSpeed = 30;
                    }
                    break;
                case Difficulty.StartUp:
                    {
                        enemySpawnWait = 45;
                        enemySpeed = 5;

                        laserSpeed = 40;
                    }
                    break;
                case Difficulty.Easy:
                    {
                        enemySpawnWait = 40;
                        enemySpeed = 10;

                        meteorSpawnWait = 40;
                        meteorSpeed = 4;

                        laserSpeed = 50;
                    }
                    break;
                case Difficulty.Medium:
                    {
                        enemySpawnWait = 35;
                        enemySpeed = 15;

                        meteorSpawnWait = 35;
                        meteorSpeed = 6;

                        laserSpeed = 60;
                    }
                    break;
                case Difficulty.Hard:
                    {
                        enemySpawnWait = 30;
                        enemySpeed = 20;

                        meteorSpawnWait = 30;
                        meteorSpeed = 8;

                        laserSpeed = 70;
                    }
                    break;
                case Difficulty.VeryHard:
                    {
                        enemySpawnWait = 25;
                        enemySpeed = 25;

                        meteorSpawnWait = 25;
                        meteorSpeed = 10;

                        laserSpeed = 80;
                    }
                    break;
                case Difficulty.Extreme:
                    {
                        enemySpawnWait = 20;
                        enemySpeed = 30;

                        meteorSpawnWait = 20;
                        meteorSpeed = 12;

                        laserSpeed = 90;
                    }
                    break;
                case Difficulty.Pro:
                    {
                        enemySpawnWait = 15;
                        enemySpeed = 35;

                        meteorSpawnWait = 15;
                        meteorSpeed = 14;

                        laserSpeed = 100;
                    }
                    break;
                default:
                    break;
            }
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
        /// Generates a random enemy.
        /// </summary>
        private void GenerateEnemy()
        {
            var newEnemy = new Enemy();

            Canvas.SetTop(newEnemy, -100);
            Canvas.SetLeft(newEnemy, rand.Next(10, (int)windowWidth - 100));
            GameCanvas.Children.Add(newEnemy);
        }

        /// <summary>
        /// Generates a random meteor.
        /// </summary>
        private void GenerateMeteor()
        {
            var newMeteor = new Meteor();

            Canvas.SetTop(newMeteor, -100);
            Canvas.SetLeft(newMeteor, rand.Next(10, (int)windowWidth - 100));
            GameCanvas.Children.Add(newMeteor);
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

        #endregion

        #region Canvas Events

        private void GameCanvas_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var currentPoint = e.GetCurrentPoint(GameCanvas);

            pointerX = currentPoint.Position.X;
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

            SetGameCanvasSize();
            SetPlayerCanvasTop();
        }

        /// <summary>
        /// Sets the window and canvas size on startup.
        /// </summary>
        void SetWindowSizeAtStartup()
        {
            windowWidth = Window.Current.Bounds.Width;
            windowHeight = Window.Current.Bounds.Height;

            SetGameCanvasSize();

            pointerX = windowWidth / 2;
        }

        /// <summary>
        /// Sets the game canvas size according to current window size.
        /// </summary>
        private void SetGameCanvasSize()
        {
            GameCanvas.Height = windowHeight;
            GameCanvas.Width = windowWidth;
        }

        #endregion

        #region Sounds

        /// <summary>
        /// Plays the background music.
        /// </summary>
        private void PlayBackgroundMusic()
        {
            var musicTrack = rand.Next(1, 4);

            string host = null;

            switch (musicTrack)
            {
                case 1: { host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/slow-trap-18565.mp3"; } break;
                case 2: { host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/space-chillout-14194.mp3"; } break;
                case 3: { host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/cinematic-space-drone-10623.mp3"; } break;
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

            if (playerHealthDecreaseAudio is null)
            {
                playerHealthDecreaseAudio = OpenSilver.Interop.ExecuteJavaScript(@"
                (function() {
                    //play audio with out html audio tag
                    var playerHealthDecreaseAudio = new Audio($0);
                    playerHealthDecreaseAudio.volume = 1.0;
                   return playerHealthDecreaseAudio;
                }())", host);
            }

            PlayAudio(playerHealthDecreaseAudio);
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
    }
}
