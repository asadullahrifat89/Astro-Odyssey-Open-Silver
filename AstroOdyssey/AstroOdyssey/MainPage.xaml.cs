using System;
using System.Collections.Generic;
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

        bool isGameRunning;

        List<Border> removableItems = new List<Border>();

        Random rand = new Random();

        int enemyCounter = 100;
        int enemylimit = 50;
        int enemySpeed = 5;

        int meteorCounter = 100;
        int meteorlimit = 50;
        int meteorSpeed = 2;

        int playerSpeed = 15;

        int score = 0;
        int playerHealth = 100;

        Rect playerHitBox;

        double windowWidth, windowHeight;
        double playerX, playerY, playerWidthHalf;

        double pointerX;

        TimeSpan frameInterval = TimeSpan.FromMilliseconds(5);
        TimeSpan laserInterval = TimeSpan.FromMilliseconds(250);

        string baseUrl;

        #endregion

        #region Ctor

        public MainPage()
        {
            this.InitializeComponent();

            this.Loaded += Window_SizeChanged_Demo_Loaded;
            this.Unloaded += Window_SizeChanged_Demo_Unloaded;

            SetWindowSize();
        }

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

            GameCanvas.Height = windowHeight;
            GameCanvas.Width = windowWidth;

            //TODO: set player y axis
            Canvas.SetTop(Player, windowHeight - 100);
        }

        void SetWindowSize()
        {
            windowWidth = Window.Current.Bounds.Width;
            windowHeight = Window.Current.Bounds.Height;

            GameCanvas.Height = windowHeight;
            GameCanvas.Width = windowWidth;
        }

        #endregion

        #region Properties

        public Border Player { get; set; }

        #endregion

        #region Methods

        #region Functionality

        private void StartGame()
        {
            SpawnPlayer();
            isGameRunning = true;
            GameLoop();
            LaserLoop();
        }

        private void StopGame()
        {
            isGameRunning = false;

            GameCanvas.Children.Clear();

            PlayButton.Visibility = Visibility.Visible;
        }

        private async void GameLoop()
        {
            while (isGameRunning)
            {
                UpdateScoreboard();

                GetPlayerCoordinates();

                SpawnEnemy();

                SpawnMeteor();

                MovePlayer();

                UpdateFrame();

                ScaleDifficulty();

                RemoveRemovables();

                CheckIfGameOver();

                await Task.Delay(frameInterval);
            }
        }

        private async void LaserLoop()
        {
            while (isGameRunning)
            {
                var newLaser = new Laser();

                Canvas.SetLeft(newLaser, Canvas.GetLeft(Player) + Player.Width / 2 - newLaser.Width / 2);
                Canvas.SetTop(newLaser, Canvas.GetTop(Player) - newLaser.Height);

                GameCanvas.Children.Add(newLaser);

                PlayLaserSound();

                await Task.Delay(laserInterval);
            }
        }

        private void GetPlayerCoordinates()
        {
            playerX = Canvas.GetLeft(Player);
            playerY = Canvas.GetTop(Player);
            playerWidthHalf = Player.Width / 2;

            playerHitBox = new Rect(playerX, playerY, Player.Width, Player.Height);
        }

        private void UpdateFrame()
        {
            foreach (var element in GameCanvas.Children.OfType<GameObject>())
            {
                if (element is Laser laser)
                {
                    // move laser up
                    Canvas.SetTop(laser, Canvas.GetTop(laser) - 20);

                    if (Canvas.GetTop(laser) < 10)
                    {
                        laser.IsMarkedToDelete = true;
                        removableItems.Add(laser);
                    }

                    Rect laserHitBox = new Rect(Canvas.GetLeft(laser), Canvas.GetTop(laser), laser.Width, laser.Height);

                    foreach (var obstacle in GameCanvas.Children.OfType<GameObject>().Where(x => x is not Laser))
                    {
                        if (obstacle is Enemy targetEnemy)
                        {
                            Rect enemyHit = new Rect(Canvas.GetLeft(obstacle), Canvas.GetTop(targetEnemy), targetEnemy.Width, targetEnemy.Height);

                            if (IntersectsWith(laserHitBox, enemyHit))
                            {
                                removableItems.Add(laser);
                                removableItems.Add(targetEnemy);
                                score++;

                                PlayEnemyDestructionSound();
                            }
                        }

                        if (obstacle is Meteor targetMeteor)
                        {
                            Rect meteorHit = new Rect(Canvas.GetLeft(targetMeteor), Canvas.GetTop(targetMeteor), targetMeteor.Width, targetMeteor.Height);

                            if (IntersectsWith(laserHitBox, meteorHit))
                            {
                                removableItems.Add(laser);
                                targetMeteor.Health--;

                                if (targetMeteor.Health <= 0)
                                {
                                    removableItems.Add(targetMeteor);
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

                    Rect enemyHitBox = new Rect(Canvas.GetLeft(enemy), Canvas.GetTop(enemy), enemy.Width, enemy.Height);

                    if (IntersectsWith(playerHitBox, enemyHitBox))
                    {
                        removableItems.Add(enemy);
                        playerHealth -= 5;

                        PlayPlayerDamageSound();
                    }
                }

                if (element is Meteor meteor)
                {
                    // move meteor down
                    Canvas.SetTop(meteor, Canvas.GetTop(meteor) + meteorSpeed);

                    Rect meteorHitBox = new Rect(Canvas.GetLeft(meteor), Canvas.GetTop(meteor), meteor.Width, meteor.Height);

                    if (IntersectsWith(playerHitBox, meteorHitBox))
                    {
                        removableItems.Add(meteor);
                        playerHealth -= 5;

                        PlayPlayerDamageSound();
                    }
                }
            }
        }

        private void UpdateScoreboard()
        {
            ScoreText.Text = "Score: " + score;
            DamageText.Text = "Health: " + playerHealth;
        }

        private void CheckIfGameOver()
        {
            // game over
            if (playerHealth <= 0)
            {
                //TODO: game over
                StopGame();
            }
        }

        private void RemoveRemovables()
        {
            removableItems.ForEach((removableItem) => { GameCanvas.Children.Remove(removableItem); });
        }

        private void SpawnEnemy()
        {
            enemyCounter -= 1;

            if (enemyCounter < 0)
            {
                CreateEnemy();
                enemyCounter = enemylimit;
            }
        }

        private void SpawnMeteor()
        {
            meteorCounter -= 1;

            if (meteorCounter < 0)
            {
                CreateMeteor();
                meteorCounter = meteorlimit;
            }
        }

        private void SpawnPlayer()
        {
            Uri uri = null;
            var playerShipType = rand.Next(1, 7);

            switch (playerShipType)
            {
                case 1:
                    uri = new Uri("ms-appx:///Assets/Images/ship_A.png", UriKind.RelativeOrAbsolute);
                    break;
                case 2:
                    uri = new Uri("ms-appx:///Assets/Images/ship_B.png", UriKind.RelativeOrAbsolute);
                    break;
                case 3:
                    uri = new Uri("ms-appx:///Assets/Images/ship_C.png", UriKind.RelativeOrAbsolute);
                    break;
                case 4:
                    uri = new Uri("ms-appx:///Assets/Images/ship_D.png", UriKind.RelativeOrAbsolute);
                    break;
                case 5:
                    uri = new Uri("ms-appx:///Assets/Images/ship_E.png", UriKind.RelativeOrAbsolute);
                    break;
                case 6:
                    uri = new Uri("ms-appx:///Assets/Images/ship_F.png", UriKind.RelativeOrAbsolute);
                    break;
                case 7:
                    uri = new Uri("ms-appx:///Assets/Images/ship_G.png", UriKind.RelativeOrAbsolute);
                    break;
            }

            var imgPlayer = new Image()
            {
                Source = new BitmapImage(uri),
                Stretch = Stretch.Uniform,
                Height = 100,
                Width = 100,
                Name = "PlayerImage",
            };

            Player = new Border()
            {
                Child = imgPlayer,
                Name = "Player",
                Background = new SolidColorBrush(Colors.Transparent),
                Height = 100,
                Width = 100,
            };

            Canvas.SetLeft(Player, 100);
            Canvas.SetTop(Player, windowHeight - 100);

            GameCanvas.Children.Add(Player);
        }

        private void ScaleDifficulty()
        {
            // noob
            if (score > 10)
            {
                enemylimit = 45;
                enemySpeed = 5;

                laserInterval = TimeSpan.FromMilliseconds(225);
            }

            // easy
            if (score > 50)
            {
                enemylimit = 40;
                enemySpeed = 10;

                laserInterval = TimeSpan.FromMilliseconds(200);
            }

            // medium
            if (score > 100)
            {
                enemylimit = 35;
                enemySpeed = 15;

                laserInterval = TimeSpan.FromMilliseconds(175);
            }

            // hard
            if (score > 300)
            {
                enemylimit = 30;
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
                    Canvas.SetLeft(Player, playerX + playerSpeed);                    
                }
            }

            // move left
            if (pointerX - playerWidthHalf < playerX - playerSpeed)
            {
                Canvas.SetLeft(Player, playerX - playerSpeed);
            }
        }

        private void CreateEnemy()
        {
            var newEnemy = new Enemy();

            Canvas.SetTop(newEnemy, -100);
            Canvas.SetLeft(newEnemy, rand.Next(10, (int)windowWidth - 100));
            GameCanvas.Children.Add(newEnemy);
        }

        private void CreateMeteor()
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
            PlayBacgroundMusic();

            StartGame();
            PlayButton.Visibility = Visibility.Collapsed;
            Application.Current.Host.Content.IsFullScreen = true;
        }

        #endregion

        #region Canvas Events

        private void GameCanvas_PointerMoved(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var currentPoint = e.GetCurrentPoint(GameCanvas);

            pointerX = currentPoint.Position.X;
        }

        #endregion

        #region Sounds

        private void PlayBacgroundMusic()
        {
            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/slow-trap-18565.mp3";

            OpenSilver.Interop.ExecuteJavaScript(@"
            (function() { 
                //play audio with out html audio tag
                var myAudio = new Audio($0);
                myAudio.loop = true;
                myAudio.play();
            }())", host);
        }

        private void PlayLaserSound()
        {
            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/beam-8-43831.mp3";

            OpenSilver.Interop.ExecuteJavaScript(@"
            (function() {
                //play audio with out html audio tag
                var myAudio = new Audio($0);
                myAudio.volume = 0.1;                
                myAudio.play();
            }())", host);
        }

        private void PlayEnemyDestructionSound()
        {
            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/explosion-36210.mp3";

            OpenSilver.Interop.ExecuteJavaScript(@"
            (function() {
                //play audio with out html audio tag
                var myAudio = new Audio($0);
                myAudio.volume = 0.8;                
                myAudio.play();
            }())", host);
        }

        private void PlayMeteorDestructionSound()
        {
            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/explosion-36210.mp3";

            OpenSilver.Interop.ExecuteJavaScript(@"
            (function() {
                //play audio with out html audio tag
                var myAudio = new Audio($0);
                myAudio.volume = 0.8;
                myAudio.play();
            }())", host);
        }

        private void PlayPlayerDamageSound()
        {
            //https://cdn.pixabay.com/download/audio/2021/08/09/audio_9788fd890e.mp3?filename=big-impact-7054.mp3
            //https://cdn.pixabay.com/download/audio/2021/08/04/audio_fadfc77b9e.mp3?filename=explosion-6055.mp3
            //https://cdn.pixabay.com/download/audio/2022/03/10/audio_c23007ce5b.mp3?filename=explosion-39897.mp3
            //https://cdn.pixabay.com/download/audio/2022/03/10/audio_745451bd70.mp3?filename=8-bit-explosion-low-resonant-45659.mp3

            var host = $"{baseUrl}resources/AstroOdyssey/Assets/Sounds/explosion-39897.mp3";

            OpenSilver.Interop.ExecuteJavaScript(@"
            (function() {
                //play audio with out html audio tag
                var myAudio = new Audio($0);
                myAudio.volume = 1.0;
                myAudio.play();
            }())", host);
        }

        #endregion

        #endregion
    }
}
