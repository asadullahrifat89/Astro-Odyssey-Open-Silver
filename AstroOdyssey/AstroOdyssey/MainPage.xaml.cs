﻿using System;
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
        int enemyframeWait = 50;
        int enemySpeed = 5;

        int meteorCounter = 100;
        int meteorframeWait = 50;
        int meteorSpeed = 2;

        int playerSpeed = 15;

        int score = 0;

        double windowWidth, windowHeight;
        double playerX, playerY, playerWidthHalf;

        double pointerX;

        TimeSpan laserInterval = TimeSpan.FromMilliseconds(250);

        string baseUrl;

        object backgroundAudio = null;
        object laserAudio = null;
        object enemyDestructionAudio = null;
        object meteorDestructionAudio = null;
        object laserHitMeteorAudio = null;
        object playerHealthDecreaseAudio = null;

        Player player;
        Rect playerHitBox;

        #endregion

        #region Ctor

        public MainPage()
        {
            this.InitializeComponent();

            this.Loaded += Window_SizeChanged_Demo_Loaded;
            this.Unloaded += Window_SizeChanged_Demo_Unloaded;

            SetWindowSize();
            GameGrid.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Methods

        #region Functionality

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

        private void StopGame()
        {
            StopBacgroundMusic();
            isGameRunning = false;
            PlayButton.Visibility = Visibility.Visible;
        }

        private async void GameLoop()
        {
            var watch = Stopwatch.StartNew();

            while (isGameRunning)
            {
                var frameStartTime = watch.ElapsedMilliseconds;

                UpdateScoreboard();

                GetPlayerCoordinates();

                SpawnEnemy();

                SpawnMeteor();

                MovePlayer();

                UpdateFrame();

                ScaleDifficulty();

                CheckPlayerHealth();

                // calculate FPS
                if (_lastFPSTime + 1000 < frameStartTime)
                {
                    _fpsCount = _fpsCounter;
                    _fpsCounter = 0;
                    _lastFPSTime = frameStartTime;
                }

                _fpsCounter++;

                var frameTime = watch.ElapsedMilliseconds - frameStartTime;
                var waitTime = Math.Max((int)(FRAME_CAP_MS - frameTime), 1);

                await Task.Delay(waitTime);
            }
        }

        private async void LaserLoop()
        {
            while (isGameRunning)
            {
                var newLaser = new Laser();

                Canvas.SetLeft(newLaser, Canvas.GetLeft(player) + player.Width / 2 - newLaser.Width / 2);
                Canvas.SetTop(newLaser, Canvas.GetTop(player) - newLaser.Height);

                GameCanvas.Children.Add(newLaser);

                PlayLaserSound();

                await Task.Delay(laserInterval);
            }
        }

        private void GetPlayerCoordinates()
        {
            playerX = Canvas.GetLeft(player);
            playerY = Canvas.GetTop(player);
            playerWidthHalf = player.Width / 2;

            playerHitBox = player.GetRect();
        }

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

                    foreach (var obstacle in obstacles)
                    {
                        if (obstacle is Enemy targetEnemy)
                        {
                            Rect enemyBounds = targetEnemy.GetRect();

                            if (IntersectsWith(laserBounds, enemyBounds))
                            {
                                removableObjects.Add(laser);

                                removableObjects.Add(targetEnemy);

                                score++;

                                PlayEnemyDestructionSound();
                            }
                        }

                        if (obstacle is Meteor targetMeteor)
                        {
                            Rect meteorBounds = targetMeteor.GetRect();

                            if (IntersectsWith(laserBounds, meteorBounds))
                            {
                                removableObjects.Add(laser);

                                targetMeteor.LooseHealth();

                                PlayLaserHitMeteorSound();

                                if (targetMeteor.IsDestroyable)
                                {
                                    removableObjects.Add(targetMeteor);

                                    PlayMeteorDestructionSound();
                                }
                            }
                        }
                    }
                }

                if (element is Enemy enemy)
                {
                    // move enemy down
                    Canvas.SetTop(enemy, Canvas.GetTop(enemy) + enemySpeed);

                    Rect enemyHitBox = enemy.GetRect();

                    if (IntersectsWith(playerHitBox, enemyHitBox))
                    {
                        removableObjects.Add(enemy);

                        player.LooseHealth();

                        PlayPlayerHealthDecreaseSound();
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

                    if (IntersectsWith(playerHitBox, meteorHitBox))
                    {
                        removableObjects.Add(meteor);

                        player.LooseHealth();

                        PlayPlayerHealthDecreaseSound();
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

            Parallel.ForEach(removableObjects, (removableItem) => { GameCanvas.Children.Remove(removableItem); });
        }

        private void UpdateScoreboard()
        {
            ScoreText.Text = "Score: " + score;
            HealthText.Text = "Health: " + player.Health;
            FPSText.Text = "FPS: " + _fpsCount;
            ObjectsText.Text = "Objects: " + GameCanvas.Children.Count();
        }

        private void CheckPlayerHealth()
        {
            // game over
            if (player.IsDestroyable)
            {
                HealthText.Text = "Health: " + 0;
                StopGame();
            }
        }

        private void SpawnEnemy()
        {
            // each frame progress decreases this counter
            enemyCounter -= 1;

            // when counter reaches zero, create an enemy
            if (enemyCounter < 0)
            {
                GenerateEnemy();
                enemyCounter = enemyframeWait;
            }
        }

        private void SpawnMeteor()
        {
            // each frame progress decreases this counter
            meteorCounter -= 1;

            // when counter reaches zero, create a meteor
            if (meteorCounter < 0)
            {
                GenerateMeteor();
                meteorCounter = meteorframeWait;
            }
        }

        private void SpawnPlayer()
        {
            player = new Player();

            Canvas.SetLeft(player, pointerX);
            Canvas.SetTop(player, windowHeight - 100);

            GameCanvas.Children.Add(player);
        }

        private void ScaleDifficulty()
        {
            // noob
            if (score > 10)
            {
                enemyframeWait = 45;
                enemySpeed = 5;

                laserInterval = TimeSpan.FromMilliseconds(225);
            }

            // easy
            if (score > 50)
            {
                enemyframeWait = 40;
                enemySpeed = 10;

                laserInterval = TimeSpan.FromMilliseconds(200);
            }

            // medium
            if (score > 100)
            {
                enemyframeWait = 35;
                enemySpeed = 15;

                laserInterval = TimeSpan.FromMilliseconds(175);
            }

            // hard
            if (score > 300)
            {
                enemyframeWait = 30;
                enemySpeed = 20;

                laserInterval = TimeSpan.FromMilliseconds(150);
            }
        }

        private void MovePlayer()
        {
            // move right
            if (pointerX - playerWidthHalf > playerX + playerSpeed)
            {
                if (playerX + 90 < windowWidth)
                {
                    Canvas.SetLeft(player, playerX + playerSpeed);
                }
            }

            // move left
            if (pointerX - playerWidthHalf < playerX - playerSpeed)
            {
                Canvas.SetLeft(player, playerX - playerSpeed);
            }
        }

        private void GenerateEnemy()
        {
            var newEnemy = new Enemy();

            Canvas.SetTop(newEnemy, -100);
            Canvas.SetLeft(newEnemy, rand.Next(10, (int)windowWidth - 100));
            GameCanvas.Children.Add(newEnemy);
        }

        private void GenerateMeteor()
        {
            var newMeteor = new Meteor();

            Canvas.SetTop(newMeteor, -100);
            Canvas.SetLeft(newMeteor, rand.Next(10, (int)windowWidth - 100));
            GameCanvas.Children.Add(newMeteor);
        }

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

        private void GameCanvas_PointerMoved(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var currentPoint = e.GetCurrentPoint(GameCanvas);

            pointerX = currentPoint.Position.X;
        }

        #endregion

        #region Window Events

        //When the window is loaded, we add the event Current_SizeChanged
        void Window_SizeChanged_Demo_Loaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SizeChanged += Current_SizeChanged;
            baseUrl = HtmlPage.Document.DocumentUri.OriginalString;
        }

        //When the window is unloaded, we remove the event Current_SizeChanged
        void Window_SizeChanged_Demo_Unloaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SizeChanged -= Current_SizeChanged;
        }

        void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            windowWidth = e.Size.Width;
            windowHeight = e.Size.Height;

            SetGameCanvasSize();

            Canvas.SetTop(player, windowHeight - 100);
        }

        void SetWindowSize()
        {
            windowWidth = Window.Current.Bounds.Width;
            windowHeight = Window.Current.Bounds.Height;

            SetGameCanvasSize();

            pointerX = windowWidth / 2;
        }

        private void SetGameCanvasSize()
        {
            GameCanvas.Height = windowHeight;
            GameCanvas.Width = windowWidth;
        }

        #endregion

        #region Sounds

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

            backgroundAudio = OpenSilver.Interop.ExecuteJavaScript(@"
            (function() { 
                //play audio with out html audio tag
                var backgroundAudio = new Audio($0);
                backgroundAudio.loop = true;
                return backgroundAudio;
            }())", host);

            PlayAudio(backgroundAudio);
        }

        private void StopBacgroundMusic()
        {
            if (backgroundAudio is not null)
            {
                PauseAudio(backgroundAudio);
            }
        }

        private void PlayLaserSound()
        {
            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/beam-8-43831.mp3";

            laserAudio = OpenSilver.Interop.ExecuteJavaScript(@"
                (function() {
                    //play audio with out html audio tag
                    var myAudio = new Audio($0);
                    myAudio.volume = 0.1;
                    return myAudio;
                }())", host);

            PlayAudio(laserAudio);
        }

        private void PlayEnemyDestructionSound()
        {
            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/explosion-36210.mp3";

            enemyDestructionAudio = OpenSilver.Interop.ExecuteJavaScript(@"
                (function() {
                    //play audio with out html audio tag
                    var myAudio = new Audio($0);
                    myAudio.volume = 0.8;                
                    return myAudio;
                }())", host);

            PlayAudio(enemyDestructionAudio);
        }

        private void PlayLaserHitMeteorSound()
        {
            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/explosion-sfx-43814.mp3";

            laserHitMeteorAudio = OpenSilver.Interop.ExecuteJavaScript(@"
            (function() {
                //play audio with out html audio tag
                var myAudio = new Audio($0);
                myAudio.volume = 0.6;
                return myAudio;
            }())", host);

            PlayAudio(laserHitMeteorAudio);
        }

        private void PlayMeteorDestructionSound()
        {
            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/explosion-36210.mp3";

            meteorDestructionAudio = OpenSilver.Interop.ExecuteJavaScript(@"
            (function() {
                //play audio with out html audio tag
                var myAudio = new Audio($0);
                myAudio.volume = 0.8;
                return myAudio;
            }())", host);

            PlayAudio(meteorDestructionAudio);
        }

        private void PlayPlayerHealthDecreaseSound()
        {
            //https://cdn.pixabay.com/download/audio/2021/08/09/audio_9788fd890e.mp3?filename=big-impact-7054.mp3
            //https://cdn.pixabay.com/download/audio/2021/08/04/audio_fadfc77b9e.mp3?filename=explosion-6055.mp3
            //https://cdn.pixabay.com/download/audio/2022/03/10/audio_c23007ce5b.mp3?filename=explosion-39897.mp3
            //https://cdn.pixabay.com/download/audio/2022/03/10/audio_745451bd70.mp3?filename=8-bit-explosion-low-resonant-45659.mp3

            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/explosion-39897.mp3";

            playerHealthDecreaseAudio = OpenSilver.Interop.ExecuteJavaScript(@"
            (function() {
                //play audio with out html audio tag
                var myAudio = new Audio($0);
                myAudio.volume = 1.0;
               return myAudio;
            }())", host);

            PlayAudio(playerHealthDecreaseAudio);
        }

        private void PlayAudio(object audio)
        {
            OpenSilver.Interop.ExecuteJavaScript(@"
            (function() { 
                //play audio with out html audio tag              
                $0.play();           
            }())", audio);
        }

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
