using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Browser;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

namespace AstroOdyssey
{
    public partial class MainPage : Page
    {
        #region Fields

        const float FRAME_CAP_MS = 1000.0f / 60.0f;
        private int _fpsCount = 0;
        private int _fpsCounter = 0;
        private float _lastFPSTime = 0;

        bool isGameRunning;

        List<GameObject> removableObjects = new List<GameObject>();

        Random rand = new Random();

        int enemyCounter = 100;
        int enemySpawnWait = 50;
        int enemySpeed = 5;

        int meteorCounter = 100;
        int meteorSpawnWait = 50;
        int meteorSpeed = 2;

        int playerSpeed = 15;

        double score = 0;

        double windowWidth, windowHeight;
        double playerX, playerWidthHalf;

        double pointerX;

        TimeSpan laserSpeed = TimeSpan.FromMilliseconds(250);

        string baseUrl;

        object backgroundAudio = null;
        object laserAudio = null;
        object enemyDestructionAudio = null;
        object meteorDestructionAudio = null;
        object laserHitMeteorAudio = null;
        object playerHealthDecreaseAudio = null;

        Player player;
        Rect playerBounds;

        #endregion

        #region Ctor

        /// <summary>
        /// Constructor
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();

            this.Loaded += Window_SizeChanged_Demo_Loaded;
            this.Unloaded += Window_SizeChanged_Demo_Unloaded;

            SetWindowSizeAtStartup();
            GameGrid.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Methods

        #region Game Methods

        /// <summary>
        /// Starts the game. Spawns the player and starts game and laser loops.
        /// </summary>
        private void StartGame()
        {
            PlayBacgroundMusic();

            score = 0;
            _fpsCount = 0;
            _fpsCounter = 0;
            _lastFPSTime = 0;

            GameCanvas.Children.Clear();

            GameGrid.Visibility = Visibility.Visible;
            PlayButton.Visibility = Visibility.Collapsed;

            SpawnPlayer();

            isGameRunning = true;

            GameLoop();
            LaserLoop();
        }

        /// <summary>
        /// Stops the game.
        /// </summary>
        private void StopGame()
        {
            StopBacgroundMusic();
            isGameRunning = false;
            PlayButton.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Runs the game loop if game is running. Updates stats, gets player bounds, spawns enemies and meteors, moves the player, updates the frame, scales difficulty, checks player health, calculates fps and frame time.
        /// </summary>
        private async void GameLoop()
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

                ScaleDifficulty();

                CheckPlayerDeath();

                CalculateFps(frameStartTime);

                int frameTime = CalculateFrameTime(watch, frameStartTime);

                await Task.Delay(frameTime);
            }
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
            if (_lastFPSTime + 1000 < frameStartTime)
            {
                _fpsCount = _fpsCounter;
                _fpsCounter = 0;
                _lastFPSTime = frameStartTime;
            }

            _fpsCounter++;
        }

        /// <summary>
        /// Runs the laser loop if game is running.
        /// </summary>
        private async void LaserLoop()
        {
            while (isGameRunning)
            {
                var newLaser = new Laser();

                Canvas.SetLeft(newLaser, Canvas.GetLeft(player) + player.Width / 2 - newLaser.Width / 2);
                Canvas.SetTop(newLaser, Canvas.GetTop(player) - newLaser.Height);

                GameCanvas.Children.Add(newLaser);

                PlayLaserSound();

                await Task.Delay(laserSpeed);
            }
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
            var gameObjects = GameCanvas.Children.OfType<GameObject>();

            Parallel.ForEach(gameObjects, (element) =>
            {
                if (element is Laser laser)
                {
                    // move laser up
                    Canvas.SetTop(laser, Canvas.GetTop(laser) - 20);

                    if (Canvas.GetTop(laser) < 10)
                    {
                        removableObjects.Add(laser);
                    }

                    Rect laserBounds = laser.GetRect();

                    var obstacles = GameCanvas.Children.OfType<GameObject>().Where(x => x is not Laser);

                    Parallel.ForEach(obstacles, (obstacle) =>
                    {
                        if (obstacle is Enemy targetEnemy)
                        {
                            Rect enemyBounds = targetEnemy.GetRect();

                            if (IntersectsWith(laserBounds, enemyBounds))
                            {
                                removableObjects.Add(laser);

                                targetEnemy.LooseHealth();

                                // move the enemy backwards a bit
                                Canvas.SetTop(targetEnemy, Canvas.GetTop(targetEnemy) - (enemySpeed * 3) / 2);

                                PlayLaserHitMeteorSound();

                                if (targetEnemy.IsDestroyable)
                                {
                                    removableObjects.Add(targetEnemy);

                                    PlayerScoreByEnemyDestruction();

                                    PlayEnemyDestructionSound();
                                }
                            }
                        }

                        if (obstacle is Meteor targetMeteor)
                        {
                            Rect meteorBounds = targetMeteor.GetRect();

                            if (IntersectsWith(laserBounds, meteorBounds))
                            {
                                removableObjects.Add(laser);

                                targetMeteor.LooseHealth();

                                // move the meteor backwards a bit
                                Canvas.SetTop(targetMeteor, Canvas.GetTop(targetMeteor) - (meteorSpeed * 4) / 2);

                                PlayLaserHitMeteorSound();

                                if (targetMeteor.IsDestroyable)
                                {
                                    removableObjects.Add(targetMeteor);

                                    PlayerScoreByMeteorDestruction();

                                    PlayMeteorDestructionSound();
                                }
                            }
                        }
                    });
                }

                if (element is Enemy enemy)
                {
                    // move enemy down
                    Canvas.SetTop(enemy, Canvas.GetTop(enemy) + enemySpeed);

                    Rect enemyHitBox = enemy.GetRect();

                    if (IntersectsWith(playerBounds, enemyHitBox))
                    {
                        removableObjects.Add(enemy);

                        player.LooseHealth();

                        PlayPlayerHealthLossSound();
                    }
                    else
                    {
                        if (Canvas.GetTop(enemy) > windowHeight)
                        {
                            removableObjects.Add(enemy);
                        }
                    }
                }

                if (element is Meteor meteor)
                {
                    // move meteor down
                    Canvas.SetTop(meteor, Canvas.GetTop(meteor) + meteorSpeed);

                    Rect meteorHitBox = meteor.GetRect();

                    if (IntersectsWith(playerBounds, meteorHitBox))
                    {
                        removableObjects.Add(meteor);

                        player.LooseHealth();

                        PlayPlayerHealthLossSound();
                    }
                    else
                    {
                        if (Canvas.GetTop(meteor) > windowHeight)
                        {
                            removableObjects.Add(meteor);
                        }
                    }
                }
            });

            Parallel.ForEach(removableObjects, (removableItem) =>
            {
                // TODO: add storyboard animation for destruction
                GameCanvas.Children.Remove(removableItem);
            });
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
            FPSText.Text = "FPS: " + _fpsCount;
            ObjectsText.Text = "Objects: " + GameCanvas.Children.Count();
        }

        /// <summary>
        /// Gets the player health points.
        /// </summary>
        /// <returns></returns>
        private string GetPlayerHealthPoints()
        {
            var healthPoints = player.Health / player.HealthSlot;
            var healthIcon = /*"•";*/ "❤️";
            var health = string.Empty;

            for (int i = 0; i < healthPoints; i++)
            {
                health = health + healthIcon;
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
        /// Scales up difficulty according to player score.
        /// </summary>
        private void ScaleDifficulty()
        {
            // startup
            if (score > 10)
            {
                enemySpawnWait = 45;
                enemySpeed = 5;

                laserSpeed = TimeSpan.FromMilliseconds(225);
            }

            // easy
            if (score > 50)
            {
                enemySpawnWait = 40;
                enemySpeed = 10;

                meteorSpawnWait = 40;
                meteorSpeed = 4;

                laserSpeed = TimeSpan.FromMilliseconds(200);
            }

            // medium
            if (score > 100)
            {
                enemySpawnWait = 35;
                enemySpeed = 15;

                meteorSpawnWait = 35;
                meteorSpeed = 6;

                laserSpeed = TimeSpan.FromMilliseconds(175);
            }

            // hard
            if (score > 300)
            {
                enemySpawnWait = 30;
                enemySpeed = 20;

                meteorSpawnWait = 30;
                meteorSpeed = 8;

                laserSpeed = TimeSpan.FromMilliseconds(150);
            }

            // very hard
            if (score > 500)
            {
                enemySpawnWait = 25;
                enemySpeed = 25;

                meteorSpawnWait = 25;
                meteorSpeed = 10;

                laserSpeed = TimeSpan.FromMilliseconds(125);
            }

            // extreme hard
            if (score > 700)
            {
                enemySpawnWait = 20;
                enemySpeed = 30;

                meteorSpawnWait = 20;
                meteorSpeed = 12;

                laserSpeed = TimeSpan.FromMilliseconds(100);
            }
        }

        /// <summary>
        /// Moves the player to last pointer pressed position by x axis.
        /// </summary>
        private void MovePlayer()
        {
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

        #region Button Events

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            StartGame();
            Application.Current.Host.Content.IsFullScreen = true;
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
        /// When the window is loaded, we add the event Current_SizeChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Window_SizeChanged_Demo_Loaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SizeChanged += Current_SizeChanged;
            baseUrl = HtmlPage.Document.DocumentUri.OriginalString;
        }

        /// <summary>
        /// When the window is unloaded, we remove the event Current_SizeChanged
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
        private void PlayBacgroundMusic()
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
        private void StopBacgroundMusic()
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
        /// Plays the sound effect when a laser hits a meteor.
        /// </summary>
        private void PlayLaserHitMeteorSound()
        {
            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/explosion-sfx-43814.mp3";

            if (laserHitMeteorAudio is null)
            {
                laserHitMeteorAudio = OpenSilver.Interop.ExecuteJavaScript(@"
                (function() {
                    //play audio with out html audio tag
                    var laserHitMeteorAudio = new Audio($0);
                    laserHitMeteorAudio.volume = 0.6;
                    return laserHitMeteorAudio;
                }())", host);
            }

            PlayAudio(laserHitMeteorAudio);
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
