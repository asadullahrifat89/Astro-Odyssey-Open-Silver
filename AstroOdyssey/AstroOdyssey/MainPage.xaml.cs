﻿using System;
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

        bool isGameRunning, isMovingLeft, isMovingRight = false;

        List<Border> itemRemover = new List<Border>();

        Random rand = new Random();

        int enemySpriteCounter = 0;
        int enemyCounter = 100;
        int playerSpeed = 10;
        int limit = 50;
        int score = 0;
        int damage = 0;
        int enemySpeed = 5;

        Rect playerHitBox;

        double windowWidth, windowHeight;

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
            MakePlayer();
            isGameRunning = true;
        }

        private async void RunGame()
        {
            while (isGameRunning)
            {
                playerHitBox = new Rect(Canvas.GetLeft(Player), Canvas.GetTop(Player), Player.Width, Player.Height);

                enemyCounter -= 1;

                ScoreText.Text = "Score: " + score;
                DamageText.Text = "Damage " + damage;

                if (enemyCounter < 0)
                {
                    MakeEnemies();
                    enemyCounter = limit;
                }

                if (isMovingLeft == true && Canvas.GetLeft(Player) > 0)
                {
                    Canvas.SetLeft(Player, Canvas.GetLeft(Player) - playerSpeed);
                }
                if (isMovingRight == true && Canvas.GetLeft(Player) + 90 < windowWidth)
                {
                    Canvas.SetLeft(Player, Canvas.GetLeft(Player) + playerSpeed);
                }

                foreach (var x in GameCanvas.Children.OfType<Border>())
                {
                    if (x is Border && (string)x.Tag == "bullet")
                    {
                        Canvas.SetTop(x, Canvas.GetTop(x) - 20);

                        Rect bulletHitBox = new Rect(Canvas.GetLeft(x), Canvas.GetTop(x), x.Width, x.Height);

                        if (Canvas.GetTop(x) < 10)
                        {
                            itemRemover.Add(x);
                        }

                        foreach (var y in GameCanvas.Children.OfType<Border>())
                        {
                            if (y is Border && (string)y.Tag == "enemy")
                            {
                                Rect enemyHit = new Rect(Canvas.GetLeft(y), Canvas.GetTop(y), y.Width, y.Height);

                                if (IntersectsWith(bulletHitBox, enemyHit))
                                {
                                    itemRemover.Add(x);
                                    itemRemover.Add(y);
                                    score++;
                                }
                            }
                        }

                    }

                    if (x is Border && (string)x.Tag == "enemy")
                    {
                        Canvas.SetTop(x, Canvas.GetTop(x) + enemySpeed);

                        Rect enemyHitBox = new Rect(Canvas.GetLeft(x), Canvas.GetTop(x), x.Width, x.Height);

                        if (IntersectsWith(playerHitBox, enemyHitBox))
                        {
                            itemRemover.Add(x);
                            damage += 5;
                        }

                    }
                }

                foreach (Border i in itemRemover)
                {
                    GameCanvas.Children.Remove(i);
                }

                if (score > 5)
                {
                    limit = 20;
                    enemySpeed = 15;
                }

                if (damage >= 100)
                {
                    //TODO: game over
                }

                await Task.Delay(TimeSpan.FromMilliseconds(10));
            }
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

        #region Player

        private void MakePlayer()
        {
            var imgPlayer = new Image()
            {
                Source = new BitmapImage(new Uri("ms-appx:///Assets/Images/ship_H.png", UriKind.RelativeOrAbsolute)),
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

        #endregion

        #region Enemy

        private void MakeEnemies()
        {
            enemySpriteCounter = rand.Next(1, 5);

            Uri uri = null;

            switch (enemySpriteCounter)
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

        #endregion

        #region Canvas Events

        private void GameCanvas_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Left)
            {
                isMovingLeft = true;
            }
            if (e.Key == Windows.System.VirtualKey.Right)
            {
                isMovingRight = true;
            }
        }

        private void GameCanvas_KeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Left)
            {
                isMovingLeft = false;
            }
            if (e.Key == Windows.System.VirtualKey.Right)
            {
                isMovingRight = false;
            }

            if (e.Key == Windows.System.VirtualKey.Space)
            {
                Border newBullet = new Border
                {
                    Tag = "bullet",
                    Height = 20,
                    Width = 5,
                    Background = new SolidColorBrush(Colors.Red),
                };

                Canvas.SetLeft(newBullet, Canvas.GetLeft(Player) + Player.Width / 2);
                Canvas.SetTop(newBullet, Canvas.GetTop(Player) - newBullet.Height);

                GameCanvas.Children.Add(newBullet);
            }
        }

        #endregion

        #endregion
    }
}
