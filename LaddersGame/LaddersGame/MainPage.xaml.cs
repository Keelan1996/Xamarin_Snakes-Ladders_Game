using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using Xamarin.Forms;

namespace LaddersGame
{

    public partial class MainPage : ContentPage
    {

        const int START_ROW = 11;
        const int START_COL = 1;
        const int EDGE_RIGHT = 10;
        const int EDGE_LEFT = 1;
        const int NUMBER_OF_SNAKES = 10;
        const int NUMBER_OF_PLAYERS = 3;
        const int NUMBER_OF_LADDERS = 11;

        Random _random;
        int _diceRoll;
        int _currentPlayer;
        int _currentEdgeCol;
        int _IsMovingLR;
        int[][] _Ladders;

        
        int[][] _Players;

        int[][] _Snakes;

        public MainPage()
        {
            InitializeComponent();
            SetupSnakes();
            SetupPlayers();
            SetupLadders();
            // set up my game variables.
            _currentPlayer = 1;
            // from the player array, get the values
            _IsMovingLR = _Players[_currentPlayer - 1][0];
            _currentEdgeCol = _Players[_currentPlayer - 1][1];
            _random = new Random();
        }

        private void SetupLadders()
        {
            _Ladders = new int[NUMBER_OF_LADDERS][] {
                                            new int[4] {3,3, 1,3},
                                            new int[4] {2, 7, 1, 7},
                                            new int[4] {3, 10, 1, 10},
                                            new int[4] {8, 8, 2, 4},
                                            new int[4] {5, 10, 4,7 },
                                            new int[4] {10, 7, 9, 7},
                                            new int[4] {10, 8, 7, 10},
                                            new int[4] {9, 6, 8, 6},
                                            new int[4] {8, 1, 6, 2},
                                            new int[4] {7, 5, 6, 4},
                                            new int[4] {10, 2, 7, 3}
            };

        }

        private void SetupPlayers()
        {

            _Players = new int[NUMBER_OF_PLAYERS][]
            {
                // stores {_IsMovingLR, _currentEdgeCol }
                new int[2] { 1, EDGE_RIGHT },
                new int[2] { 1, EDGE_RIGHT },
                new int[2] { 1, EDGE_RIGHT }
            };
        }

        private void SetupSnakes()
        {
            // initialise the global variable for snakes here
            // this adds a single dimensional (simple) array for each snake.
            // Each snake is represented as int[4] { TopRow, TopCol, BottomRow, BottomCol }
            _Snakes = new int[NUMBER_OF_SNAKES][] {
                                                    new int[4] {1, 2, 3, 1 },
                                                    new int[4] {1, 6, 3, 6 },
                                                    new int[4] {1, 9, 2, 8 },
                                                    new int[4] {2, 9, 4, 8 },
                                                    new int[4] {3, 7, 5, 8 },
                                                    new int[4] {4, 2, 9, 2 },
                                                    new int[4] {4, 4, 5, 1 },
                                                    new int[4] {6, 6, 8, 5},
                                                    new int[4] {6, 9, 9, 10},
                                                    new int[4] {9, 5, 10, 6}
                                                  };
        }

        private async void btnDiceRoll_Clicked(object sender, EventArgs e)
        {
            // generate random number
            
            _diceRoll = _random.Next(1, 7);
            // update the dice roll label
            lblDiceRoll.Text = _diceRoll.ToString();
            lblUpdates.Text = "P " + _currentPlayer;
            // move piece
            await MovePiece();

            // get the next player.
            _currentPlayer++;
            if (_currentPlayer == 4) { _currentPlayer = 1; }
            // from the player array, get the values
            _IsMovingLR = _Players[_currentPlayer - 1][0];
            _currentEdgeCol = _Players[_currentPlayer - 1][1];

        }

        #region PIECE MOVEMENT CODE
        private async Task MovePiece()  // async marks the MovePiece method as using an await call
        {
            // if it's the first move, put the row = 10
            // which piece am I moving - x:Name="bvP1"
            // FindByName - looks for x:Name properties in XAML
            string currPlayer = "bvP" + _currentPlayer.ToString();
            BoxView bvPlayer = (BoxView)GrdBoard.FindByName(currPlayer);
            int currPlayerCol = (int)bvPlayer.GetValue(Grid.ColumnProperty);

            // is this the first move.
            if (START_ROW == (int)bvPlayer.GetValue(Grid.RowProperty))
            {
                bvPlayer.SetValue(Grid.RowProperty, START_ROW - 1);
                bvPlayer.SetValue(Grid.ColumnProperty, START_COL);
                _diceRoll--;
                await MoveHorizontal(bvPlayer, _diceRoll);
                // CheckForLadders();
                return;
            }

            // if I get to here, then it's a later move
            // if edgeCol - player Col <= dice roll, then simple move from L -> R
            //if( _diceRoll <= Math.Abs(_currentEdgeCol - currPlayerCol) )
            if (_diceRoll <= Math.Abs(_currentEdgeCol - currPlayerCol))
            {
                // translate my player piece
                await MoveHorizontal(bvPlayer, _diceRoll);
            }
            else // move around a corner
            {
                // Left to Right movement of diff
                int diff = Math.Abs(_currentEdgeCol - currPlayerCol);
                await MoveHorizontal(bvPlayer, diff);
                _diceRoll -= diff;  // decrement diceroll by diff

                // move vertically
                await MoveVerticalOneRow(bvPlayer);
                _diceRoll--;

                // move R-L with what's left on the dice roll (subtract columns)
                await MoveHorizontal(bvPlayer, _diceRoll);
            }


            CheckForSnakes(bvPlayer);
            CheckForLadders(bvPlayer);
        }

        private async Task MoveHorizontal(BoxView bvPlayer, int numSpaces)
        {
            int horizontalDistance = ((int)GrdBoard.Width / 12) * numSpaces * _IsMovingLR;
            uint timeValue = (uint)(Math.Abs(numSpaces) * 150);
            int currPlayerCol = (int)bvPlayer.GetValue(Grid.ColumnProperty);

            await bvPlayer.TranslateTo(horizontalDistance, 0, timeValue);   // takes time, so ask the system to wait for it to finish
            bvPlayer.TranslationX = 0;
            // set the Col to the curr + dice roll
            bvPlayer.SetValue(Grid.ColumnProperty, currPlayerCol + (numSpaces * _IsMovingLR));
        }

        private async Task MoveVerticalOneRow(BoxView pieceToMove)
        {
            int verticalDistance = (int)GrdBoard.Width / 12;    // 1 square
            uint timeValue = (uint)150;
            await pieceToMove.TranslateTo(0, -1 * verticalDistance, timeValue);   // takes time, so ask the system to wait for it to finish
            pieceToMove.TranslationY = 0;
            pieceToMove.SetValue(Grid.RowProperty, (int)pieceToMove.GetValue(Grid.RowProperty) - 1);

            ChangeDirection();
        }

        private void ChangeDirection()
        {
            _IsMovingLR *= -1;
            //                       if LR = -1
            //                          1           10
            _currentEdgeCol = Math.Max(EDGE_LEFT, EDGE_RIGHT * _IsMovingLR);
            // update the player array
            _Players[_currentPlayer - 1][0] = _IsMovingLR;
            _Players[_currentPlayer - 1][1] = _currentEdgeCol;
        }
        #endregion

        #region CHECK FOR SNAKES, LADDERS, WIN CONDITION
        /// <summary>
        /// checks if the player position is currently the head of a snake.
        /// </summary>
        private void CheckForSnakes(BoxView player)
        {
            int iCounter = 0;
            int indexRTop = 0, indexCTop = 1;
            int indexRBottom = 2, indexCBottom = 3;
            int playerRow, playerCol;
            int deltaRows = 0;

            playerRow = (int)player.GetValue(Grid.RowProperty);
            playerCol = (int)player.GetValue(Grid.ColumnProperty);

            // loop through the array of snakes to check if the Row and Col are equal

            while (iCounter < NUMBER_OF_SNAKES)
            {
                // playerRow == _Snakes[iCounter][indexRTop]
                if ((playerRow == _Snakes[iCounter][indexRTop]) &&
                    (playerCol == _Snakes[iCounter][indexCTop]))
                {
                    // at the top of a snake.
                    lblUpdates.Text = "snake!";
                    // if true - then player is moved back to the bottom of the snake.
                    player.SetValue(Grid.RowProperty, _Snakes[iCounter][indexRBottom]);
                    player.SetValue(Grid.ColumnProperty, _Snakes[iCounter][indexCBottom]);
                    deltaRows = _Snakes[iCounter][indexRBottom] - _Snakes[iCounter][indexRTop];
                }
                iCounter++;
            }
            // if move down an odd number of rows, change direction
            if (deltaRows % 2 == 1)
            {
                ChangeDirection();
            }
        }
        private void CheckForLadders(BoxView player)
        {
            int iCounter = 0;
            int indexRTop = 0, indexCTop = 1;
            int indexRBottom = 2, indexCBottom = 3;
            int playerRow, playerCol;
            int deltaRows = 0;
            playerRow = (int)player.GetValue(Grid.RowProperty);
            playerCol = (int)player.GetValue(Grid.ColumnProperty);
            // LOOP THROUGH LADDERS ARRAY
            while (iCounter < NUMBER_OF_LADDERS)
            {
                if ((playerRow == _Ladders[iCounter][indexRTop]) &&
                    (playerCol == _Ladders[iCounter][indexCTop]))
                {
                    lblUpdates.Text = "ladder!";
                    // MOVE THEM UP
                    player.SetValue(Grid.RowProperty, _Ladders[iCounter][indexRBottom]);
                    player.SetValue(Grid.ColumnProperty, _Ladders[iCounter][indexCBottom]);
                    deltaRows = _Ladders[iCounter][indexRTop] - _Ladders[iCounter][indexRBottom];
                }
                iCounter++;
            }
            if ((deltaRows % 2) == 1)
            {
                ChangeDirection();
            }
        }

        private void CheckForWin(BoxView player)
        {
            

        }
        #endregion
    }
}
