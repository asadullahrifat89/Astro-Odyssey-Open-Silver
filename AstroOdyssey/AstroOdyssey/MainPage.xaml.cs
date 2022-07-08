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
        int playerSpeed = 10;
        int enemylimit = 50;
        int score = 0;
        int damage = 0;
        int enemySpeed = 5;

        Rect playerHitBox;

        double windowWidth, windowHeight;

        double pointerX;

        DispatcherTimer shotTimer;

        TimeSpan frameTime = TimeSpan.FromMilliseconds(10);
        TimeSpan shotTime = TimeSpan.FromMilliseconds(200);

        //MediaElement mediaElementForLaser;

        #endregion

        #region Ctor

        public MainPage()
        {
            this.InitializeComponent();

            this.Loaded += Window_SizeChanged_Demo_Loaded;
            this.Unloaded += Window_SizeChanged_Demo_Unloaded;

            SetWindowSize();
            StartGame();
            RunGame();

            MediaElementForBackground.Play();
        }

        //When the window is loaded, we add the event Current_SizeChanged
        void Window_SizeChanged_Demo_Loaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SizeChanged += Current_SizeChanged;
            MediaElementForBackground.Play();
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

            shotTimer = new DispatcherTimer();
            shotTimer.Interval = shotTime;
            shotTimer.Tick += Timer_Tick;
            shotTimer.Start();
        }

        private void Timer_Tick(object sender, object e)
        {
            Border newBullet = new Border
            {
                Tag = "bullet",
                Height = 20,
                Width = 5,
                Background = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(50)
            };

            Canvas.SetLeft(newBullet, Canvas.GetLeft(Player) + Player.Width / 2 - newBullet.Width / 2);
            Canvas.SetTop(newBullet, Canvas.GetTop(Player) - newBullet.Height);

            GameCanvas.Children.Add(newBullet);
        }

        private async void RunGame()
        {
            while (isGameRunning)
            {
                var playerX = Canvas.GetLeft(Player);
                var playerY = Canvas.GetTop(Player);
                var playerWidthHalf = Player.Width / 2;

                playerHitBox = new Rect(playerX, playerY, Player.Width, Player.Height);

                enemyCounter -= 1;

                ScoreText.Text = "Score: " + score;
                DamageText.Text = "Damage " + damage;

                if (enemyCounter < 0)
                {
                    SpawnEnemies();
                    enemyCounter = enemylimit;
                }

                // move right
                if (pointerX - playerWidthHalf > playerX + 10)
                {
                    if (playerX + 90 < windowWidth)
                    {
                        Canvas.SetLeft(Player, playerX + playerSpeed);
                    }
                }

                // move left
                if (pointerX - playerWidthHalf < playerX - 10)
                {
                    Canvas.SetLeft(Player, playerX - playerSpeed);
                }

                foreach (var x in GameCanvas.Children.OfType<Border>())
                {
                    if (x is Border && (string)x.Tag == "bullet")
                    {
                        Canvas.SetTop(x, Canvas.GetTop(x) - 20);

                        Rect bulletHitBox = new Rect(Canvas.GetLeft(x), Canvas.GetTop(x), x.Width, x.Height);

                        if (Canvas.GetTop(x) < 10)
                        {
                            removableItems.Add(x);
                        }

                        foreach (var y in GameCanvas.Children.OfType<Border>())
                        {
                            if (y is Border && (string)y.Tag == "enemy")
                            {
                                Rect enemyHit = new Rect(Canvas.GetLeft(y), Canvas.GetTop(y), y.Width, y.Height);

                                if (IntersectsWith(bulletHitBox, enemyHit))
                                {
                                    removableItems.Add(x);
                                    removableItems.Add(y);
                                    score++;
                                }
                            }
                        }
                    }

                    if (x is Border && (string)x.Tag == "enemy")
                    {
                        //TODO: set random speed
                        Canvas.SetTop(x, Canvas.GetTop(x) + enemySpeed);

                        Rect enemyHitBox = new Rect(Canvas.GetLeft(x), Canvas.GetTop(x), x.Width, x.Height);

                        if (IntersectsWith(playerHitBox, enemyHitBox))
                        {
                            removableItems.Add(x);
                            damage += 5;
                        }

                    }
                }

                foreach (Border i in removableItems)
                {
                    GameCanvas.Children.Remove(i);
                }

                // easy
                if (score > 50)
                {
                    enemylimit = 45;
                    enemySpeed = 7;
                }

                // medium
                if (score > 100)
                {
                    enemylimit = 35;
                    enemySpeed = 10;
                }

                // hard
                if (score > 300)
                {
                    enemylimit = 20;
                    enemySpeed = 15;
                }

                // game over
                if (damage >= 100)
                {
                    //TODO: game over
                }

                await Task.Delay(frameTime);
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

        private void SpawnEnemies()
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

        #region Canvas Events

        private void GameCanvas_PointerMoved(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var currentPoint = e.GetCurrentPoint(GameCanvas);

            pointerX = currentPoint.Position.X;
        }

        #endregion

        #endregion
    }
}
