using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
        int playerSpeed = 12;
        int enemylimit = 50;
        int score = 0;
        int damage = 0;
        int enemySpeed = 5;

        Rect playerHitBox;

        double windowWidth, windowHeight;
        double playerX, playerY, playerWidthHalf;

        double pointerX;

        TimeSpan frameTime = TimeSpan.FromMilliseconds(10);
        TimeSpan laserTime = TimeSpan.FromMilliseconds(250);

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

            //TODO: set player y axis
            Canvas.SetTop(Player, windowHeight - 100);
        }

        void SetWindowSize()
        {
            windowWidth = Window.Current.Bounds.Width;
            windowHeight = Window.Current.Bounds.Height;
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

                playerX = Canvas.GetLeft(Player);
                playerY = Canvas.GetTop(Player);
                playerWidthHalf = Player.Width / 2;

                playerHitBox = new Rect(playerX, playerY, Player.Width, Player.Height);

                SpawnEnemy();

                MovePlayer();

                UpdateFrame();

                ScaleDifficulty();

                RemoveRemovables();

                CheckIfGameOver();

                await Task.Delay(frameTime);
            }
        }

        private void UpdateScoreboard()
        {
            ScoreText.Text = "Score: " + score;
            DamageText.Text = "Damage " + damage;
        }

        private void CheckIfGameOver()
        {
            // game over
            if (damage >= 100)
            {
                //TODO: game over
                StopGame();
            }
        }

        private void RemoveRemovables()
        {
            foreach (Border removableItem in removableItems)
            {
                GameCanvas.Children.Remove(removableItem);
            }
        }

        private async void LaserLoop()
        {
            while (isGameRunning)
            {
                Border newBullet = new Border
                {
                    Tag = "laser",
                    Height = 20,
                    Width = 5,
                    Background = new SolidColorBrush(Colors.White),
                    CornerRadius = new CornerRadius(50)
                };

                Canvas.SetLeft(newBullet, Canvas.GetLeft(Player) + Player.Width / 2 - newBullet.Width / 2);
                Canvas.SetTop(newBullet, Canvas.GetTop(Player) - newBullet.Height);

                GameCanvas.Children.Add(newBullet);

                PlayLaserSound();

                await Task.Delay(laserTime);
            }
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

                laserTime = TimeSpan.FromMilliseconds(225);
            }

            // easy
            if (score > 50)
            {
                enemylimit = 40;
                enemySpeed = 10;

                laserTime = TimeSpan.FromMilliseconds(200);
            }

            // medium
            if (score > 100)
            {
                enemylimit = 35;
                enemySpeed = 15;

                laserTime = TimeSpan.FromMilliseconds(175);
            }

            // hard
            if (score > 300)
            {
                enemylimit = 30;
                enemySpeed = 20;

                laserTime = TimeSpan.FromMilliseconds(150);
            }
        }

        private void UpdateFrame()
        {
            foreach (var element in GameCanvas.Children.OfType<Border>())
            {
                if (element is Border && (string)element.Tag == "laser")
                {
                    // move laser up
                    Canvas.SetTop(element, Canvas.GetTop(element) - 20);

                    if (Canvas.GetTop(element) < 10)
                    {
                        removableItems.Add(element);
                    }

                    Rect bulletHitBox = new Rect(Canvas.GetLeft(element), Canvas.GetTop(element), element.Width, element.Height);

                    foreach (var enemy in GameCanvas.Children.OfType<Border>().Where(x => (string)x.Tag == "enemy"))
                    {
                        Rect enemyHit = new Rect(Canvas.GetLeft(enemy), Canvas.GetTop(enemy), enemy.Width, enemy.Height);

                        if (IntersectsWith(bulletHitBox, enemyHit))
                        {
                            removableItems.Add(element);
                            removableItems.Add(enemy);
                            score++;

                            PlayEnemyShipDestructionSound();
                        }
                    }
                }

                if (element is Border && (string)element.Tag == "enemy")
                {
                    // move enemy down
                    Canvas.SetTop(element, Canvas.GetTop(element) + enemySpeed);

                    Rect enemyHitBox = new Rect(Canvas.GetLeft(element), Canvas.GetTop(element), element.Width, element.Height);

                    if (IntersectsWith(playerHitBox, enemyHitBox))
                    {
                        removableItems.Add(element);
                        damage += 5;
                    }
                }
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
            Uri uri = null;

            var enemyShipType = rand.Next(1, 5);

            switch (enemyShipType)
            {
                case 1:
                    uri = new Uri("ms-appx:///Assets/Images/enemy_A.png", UriKind.RelativeOrAbsolute);
                    break;
                case 2:
                    uri = new Uri("ms-appx:///Assets/Images/enemy_B.png", UriKind.RelativeOrAbsolute);
                    break;
                case 3:
                    uri = new Uri("ms-appx:///Assets/Images/enemy_C.png", UriKind.RelativeOrAbsolute);
                    break;
                case 4:
                    uri = new Uri("ms-appx:///Assets/Images/enemy_D.png", UriKind.RelativeOrAbsolute);
                    break;
                case 5:
                    uri = new Uri("ms-appx:///Assets/Images/enemy_E.png", UriKind.RelativeOrAbsolute);
                    break;
            }

            var imgEnemy = new Image()
            {
                Source = new BitmapImage(uri),
                Stretch = Stretch.Uniform,
                Height = 100,
                Width = 100,
                Name = "EnemyImage",
            };

            Border newEnemy = new Border
            {
                Tag = "enemy",
                Height = 100,
                Width = 100,
                Child = imgEnemy,
            };

            Canvas.SetTop(newEnemy, -100);
            Canvas.SetLeft(newEnemy, rand.Next(10, (int)windowWidth - 100));
            GameCanvas.Children.Add(newEnemy);
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
            OpenSilver.Interop.ExecuteJavaScript(@"
            (function() { 
                //play audio with out html audio tag
                var myAudio = new Audio('https://cdn.pixabay.com/download/audio/2022/02/10/audio_fc48af67b2.mp3?filename=slow-trap-18565.mp3');
                myAudio.play();
            }())
            ");
        }

        private void PlayLaserSound()
        {
            OpenSilver.Interop.ExecuteJavaScript(@"
            (function() {
                //play audio with out html audio tag
                var myAudio = new Audio('https://cdn.pixabay.com/download/audio/2022/03/10/audio_7bd2768f54.mp3?filename=beam-8-43831.mp3');
                myAudio.volume = 0.1;
                myAudio.play();
            }())
            ");
        }

        private void PlayEnemyShipDestructionSound()
        {
            OpenSilver.Interop.ExecuteJavaScript(@"
            (function() {
                //play audio with out html audio tag
                var myAudio = new Audio('https://cdn.pixabay.com/download/audio/2022/03/10/audio_f180bb8ad1.mp3?filename=explosion-36210.mp3');
                myAudio.volume = 0.8;
                myAudio.play();
            }())
            ");
        }

        #endregion

        #endregion
    }
}
