using SFML.Graphics;
using SFML.Window;
using SFML.System;
using SFML_Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.UI;

namespace Client.States
{
    public class LoginState : State
    {
        #region Members
        enum InputState
        {
            None,
            PlayerName,
            Hostname,
            Port,
            NametagColor,
            PlayerColor
        }
        InputState inputState = InputState.None;

        // Resources
        Color defaultColor = Color.White;
        Color highlightedColor = Color.Yellow;
        Texture colorPickerTexture;
        Texture brightnessPickerTexture;

        // Texts
        Text title;
        Text playerNameLabel;
        Text hostnameLabel;
        Text portLabel;
        Text nametagColorLabel;
        Text playerColorLabel;

        // InputFields
        Text playerNameInput;
        Text hostnameInput;
        Text portInput;

        // Separator line
        RectangleShape separatorLine;

        // Sliders
        Slider nametagColorSlider;
        Slider playerColorSlider;

        // Buttons
        Button connectButton;
        Button saveConfigButton;
        Button resetButton;

        // Preview PlayerEntity
        PlayerEntity playerPreview;

        // Colors
        System.Drawing.Color nametagSystemColor;
        Color nametagColor;
        System.Drawing.Color playerSystemColor;
        #endregion

        public LoginState(Game game) : base(game)
        {
            #region Resources
            // Load Font
            if(!game.fonts.ContainsKey("montserrat"))
            {
                game.fonts.Add("montserrat", new Font("res/Montserrat-Bold.ttf"));
            }


            // ColorPicker & BrightnessPicker Textures
            colorPickerTexture = new Texture("res/colorpicker.png");
            brightnessPickerTexture = new Texture("res/brightnesspicker.png");
            #endregion

            #region Texts
            // Screen title
            title = new Text("Networking Test", game.fonts["montserrat"], 42);
            title.Position = new Vector2f(16, 16);
            title.FillColor = defaultColor;

            // Player name label
            playerNameLabel = new Text("Name:", game.fonts["montserrat"], 32);
            playerNameLabel.Position = new Vector2f(16, 96);
            playerNameLabel.FillColor = defaultColor;

            // Nametag color label
            nametagColorLabel = new Text("Nametag:", game.fonts["montserrat"], 32);
            nametagColorLabel.Position = new Vector2f(16, playerNameLabel.Position.Y + playerNameLabel.CharacterSize + 16);
            nametagColorLabel.FillColor = defaultColor;

            // Player color label
            playerColorLabel = new Text("Player:", game.fonts["montserrat"], 32);
            playerColorLabel.Position = new Vector2f(16, nametagColorLabel.Position.Y + nametagColorLabel.CharacterSize + 16);
            playerColorLabel.FillColor = defaultColor;

            // Hostname label
            hostnameLabel = new Text("Host:", game.fonts["montserrat"], 32);
            hostnameLabel.Position = new Vector2f(16, playerColorLabel.Position.Y + 2 * playerColorLabel.CharacterSize + 16);
            hostnameLabel.FillColor = defaultColor;

            // Port label
            portLabel = new Text("Port:", game.fonts["montserrat"], 32);
            portLabel.Position = new Vector2f(16, hostnameLabel.Position.Y + hostnameLabel.CharacterSize + 16);
            portLabel.FillColor = defaultColor;
            #endregion

            #region InputFields
            // Player name input
            playerNameInput = new Text(Config.data.name, game.fonts["montserrat"], 32);
            playerNameInput.Position = playerNameLabel.Position + new Vector2f(playerNameLabel.GetLocalBounds().Width + 16, 0);
            playerNameInput.FillColor = defaultColor;

            // Hostname input
            hostnameInput = new Text(Config.data.hostname, game.fonts["montserrat"], 32);
            hostnameInput.Position = hostnameLabel.Position + new Vector2f(hostnameLabel.GetLocalBounds().Width + 16, 0);
            hostnameInput.FillColor = defaultColor;

            // Port input
            portInput = new Text(Config.data.port.ToString(), game.fonts["montserrat"], 32);
            portInput.Position = portLabel.Position + new Vector2f(portLabel.GetLocalBounds().Width + 16, 0);
            portInput.FillColor = defaultColor;
            #endregion

            #region Separator line
            separatorLine = new RectangleShape(new Vector2f(game.window.Size.X, 4));
            separatorLine.Origin = new Vector2f(0, separatorLine.Size.Y / 2.0f);
            separatorLine.Position = new Vector2f(0, playerColorLabel.Position.Y + 1.5f * playerColorLabel.CharacterSize + 16);
            #endregion

            #region Sliders
            // Nametag color picker
            nametagColorSlider = new Slider(
                nametagColorLabel.Position + new Vector2f(Math.Max(playerColorLabel.GetLocalBounds().Width, nametagColorLabel.GetLocalBounds().Width) + 16, 4),
                new Vector2f(360, nametagColorLabel.CharacterSize - 2),
                nametagColorLabel.CharacterSize / 3.0f,
                defaultColor, highlightedColor,
                colorPickerTexture
            );

            // Player color picker
            playerColorSlider = new Slider(
                playerColorLabel.Position + new Vector2f(Math.Max(playerColorLabel.GetLocalBounds().Width, nametagColorLabel.GetLocalBounds().Width) + 16, 4),
                new Vector2f(360, playerColorLabel.CharacterSize - 2),
                playerColorLabel.CharacterSize / 3.0f,
                defaultColor, highlightedColor,
                colorPickerTexture
            );
            #endregion

            #region Buttons
            float quarterWidth = game.window.Size.X / 4.0f;
            connectButton = new Button("Connect", game.fonts["montserrat"], 24, new Vector2f(1 * quarterWidth, 1.3f * portLabel.Position.Y), defaultColor, highlightedColor);
            saveConfigButton = new Button("Save Config", game.fonts["montserrat"], 24, new Vector2f(2 * quarterWidth, 1.3f * portLabel.Position.Y), defaultColor, highlightedColor);
            resetButton = new Button("Reset", game.fonts["montserrat"], 24, new Vector2f(3 * quarterWidth, 1.3f * portLabel.Position.Y), Color.Red, highlightedColor);
            #endregion

            #region Preview PlayerEntity
            // Player preview entity
            playerPreview = new PlayerEntity(string.Empty, game.fonts["montserrat"], new Vector2f(game.window.Size.X - 64 - 16, playerNameLabel.Position.Y + 64), Config.data.playerHue, Config.data.nametagHue);
            #endregion

            #region Load colors and initialize slider positions
            // Load and apply config colors
            UpdateNametagColor(Config.data.nametagHue);
            UpdatePlayerColor(Config.data.playerHue);

            // Set slider positions according to hue
            nametagColorSlider.SetSliderPosition((Config.data.nametagHue % 360.0f) / 360.0f);
            playerColorSlider.SetSliderPosition((Config.data.playerHue % 360.0f) / 360.0f);
            #endregion

            #region Window event handling
            game.window.TextEntered += HandleTextInput;
            game.window.MouseButtonPressed += HandleMouseButtonDown;
            game.window.MouseButtonReleased += HandleMouseButtonUp;
            game.window.MouseMoved += HandleMouseMoved;
            #endregion
        }

        ~LoginState()
        {

        }

        private void HandleTextInput(object sender, TextEventArgs e)
        {
            switch(inputState)
            {
                // Ignore text input
                case InputState.None:
                default:
                    break;

                // Write into the player name
                case InputState.PlayerName:
                    if (e.Unicode == "\b")
                    {
                        if (playerNameInput.DisplayedString.Length > 0)
                        {
                            playerNameInput.DisplayedString = playerNameInput.DisplayedString.Remove(playerNameInput.DisplayedString.Length - 1);
                        }
                    } else
                    {
                        playerNameInput.DisplayedString += e.Unicode;
                    }
                    break;
                // Write into the hostname
                case InputState.Hostname:
                    if (e.Unicode == "\b")
                    {
                        if (hostnameInput.DisplayedString.Length > 0)
                        {
                            hostnameInput.DisplayedString = hostnameInput.DisplayedString.Remove(hostnameInput.DisplayedString.Length - 1);
                        }
                    }
                    else
                    {
                        hostnameInput.DisplayedString += e.Unicode;
                    }
                    break;
                // Write into the port
                case InputState.Port:
                    if (e.Unicode == "\b")
                    {
                        if (portInput.DisplayedString.Length > 0)
                        {
                            portInput.DisplayedString = portInput.DisplayedString.Remove(portInput.DisplayedString.Length - 1);
                        }
                    }
                    else
                    {
                        if(e.Unicode.All(char.IsDigit))
                            portInput.DisplayedString += e.Unicode;
                    }
                    break;
            }
        }

        private void HandleMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!game.stateMachine.IsCurrent(this))
            {
                return;
            }

            // Select player name input
            if (playerNameInput.GetGlobalBounds().Contains(e.X, e.Y))
            {
                inputState = InputState.PlayerName;
                playerNameInput.FillColor = highlightedColor;
            }
            // Select hostname input
            else if (hostnameInput.GetGlobalBounds().Contains(e.X, e.Y))
            {
                inputState = InputState.Hostname;
                hostnameInput.FillColor = highlightedColor;
            }
            // Select port input
            else if (portInput.GetGlobalBounds().Contains(e.X, e.Y))
            {
                inputState = InputState.Port;
                portInput.FillColor = highlightedColor;
            } 
            // Select and position nametag color picker & slider
            else if(nametagColorSlider.Contains(e.X, e.Y) && inputState != InputState.PlayerColor)
            {
                inputState = InputState.NametagColor;
                nametagColorSlider.Select();
                nametagColorSlider.SetSliderPositionFromWorldPos(e.X);
                int hue = (int)MathF.Round(nametagColorSlider.GetValue() * 360.0f);
                UpdateNametagColor(hue);
            } 
            // Select and position player color picker & slider
            else if (playerColorSlider.Contains(e.X, e.Y) && inputState != InputState.NametagColor)
            {
                inputState = InputState.PlayerColor;
                playerColorSlider.Select();
                playerColorSlider.SetSliderPositionFromWorldPos(e.X);
                int hue = (int)MathF.Round(playerColorSlider.GetValue() * 360.0f);
                UpdatePlayerColor(hue);
            }
            // Deselect everything
            else
            {
                inputState = InputState.None;
                playerNameInput.FillColor = nametagColor;
                hostnameInput.FillColor = defaultColor;
                portInput.FillColor = defaultColor;
            }

            // Detect button click
            if (connectButton.Contains(e.X, e.Y))
            {
                Config.data.name = playerNameInput.DisplayedString.Trim();
                Config.data.nametagHue = (int)MathF.Round(nametagSystemColor.GetHue());
                Config.data.playerHue = (int)MathF.Round(playerSystemColor.GetHue());
                Config.data.hostname = hostnameInput.DisplayedString.Trim();
                if (!short.TryParse(portInput.DisplayedString.Trim(), out Config.data.port))
                {
                    Console.WriteLine("Port must be in range [0-65535]");
                }
                else
                {
                    //Console.Clear();
                    game.stateMachine.ReplaceCurrent(new GameState(game));
                }
            }
            if (saveConfigButton.Contains(e.X, e.Y))
            {
                Config.data.name = playerNameInput.DisplayedString.Trim();
                Config.data.nametagHue = (int)MathF.Round(nametagSystemColor.GetHue());
                Config.data.playerHue = (int)MathF.Round(playerSystemColor.GetHue());
                Config.data.hostname = hostnameInput.DisplayedString.Trim();
                if (!short.TryParse(portInput.DisplayedString.Trim(), out Config.data.port))
                {
                    Console.WriteLine("Port must be in range [0-65535]");
                }
                else
                {
                    Serialiser.SaveConfigFile(Config.data);
                    Console.WriteLine("Saved config file");
                }
            }
            if (resetButton.Contains(e.X, e.Y))
            {
                Config.data = new ConfigFile();

                playerNameInput.DisplayedString = Config.data.name;
                hostnameInput.DisplayedString = Config.data.hostname;
                portInput.DisplayedString = Config.data.port.ToString();

                UpdateNametagColor(Config.data.nametagHue);
                nametagColorSlider.SetSliderPosition((Config.data.nametagHue % 360.0f) / 360.0f);

                UpdatePlayerColor(Config.data.playerHue);
                playerColorSlider.SetSliderPosition((Config.data.playerHue % 360.0f) / 360.0f);

                Console.WriteLine("Reset inputs");
            }
        }

        private void HandleMouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (inputState == InputState.NametagColor)
            {
                inputState = InputState.None;
                nametagColorSlider.Deselect();
            } 
            else if(inputState == InputState.PlayerColor)
            {
                inputState = InputState.None;
                playerColorSlider.Deselect();
            }
        }

        private void HandleMouseMoved(object sender, MouseMoveEventArgs e)
        {
            // PlayerNameInput MouseOver Highlight
            if (playerNameInput.GetGlobalBounds().Contains(e.X, e.Y))
            {
                playerNameInput.FillColor = highlightedColor;
            }
            else if (inputState != InputState.PlayerName)
            {
                playerNameInput.FillColor = nametagColor;
            }

            // HostnameInput MouseOver Highlight
            if (hostnameInput.GetGlobalBounds().Contains(e.X, e.Y))
            {
                hostnameInput.FillColor = highlightedColor;
            }
            else if (inputState != InputState.Hostname)
            {
                hostnameInput.FillColor = defaultColor;
            }

            // PortInput MouseOver Highlight
            if (portInput.GetGlobalBounds().Contains(e.X, e.Y))
            {
                portInput.FillColor = highlightedColor;
            }
            else if (inputState != InputState.Port)
            {
                portInput.FillColor = defaultColor;
            }

            // Fill ColorPicker
            if(nametagColorSlider.Contains(e.X, e.Y) && inputState != InputState.PlayerColor)
            {
                nametagColorSlider.Select();
            } 
            else if(inputState != InputState.NametagColor)
            {
                nametagColorSlider.Deselect();
            } 
            // Move Slider
            if(inputState == InputState.NametagColor)
            {
                nametagColorSlider.SetSliderPositionFromWorldPos(e.X);
                int hue = (int)MathF.Round(nametagColorSlider.GetValue() * 360.0f);
                UpdateNametagColor(hue);
            }

            // Outline ColorPicker
            if (playerColorSlider.Contains(e.X, e.Y) && inputState != InputState.NametagColor)
            {
                playerColorSlider.Select();
            }
            else if (inputState != InputState.PlayerColor)
            {
                playerColorSlider.Deselect();
            }
            // Move Slider
            if(inputState == InputState.PlayerColor)
            {
                playerColorSlider.SetSliderPositionFromWorldPos(e.X);
                int hue = (int)MathF.Round(playerColorSlider.GetValue() * 360.0f);
                UpdatePlayerColor(hue);
            }

            // ConnectButton MouseOver Highlight
            if(connectButton.Contains(e.X, e.Y))
            {
                connectButton.Select();
            } else
            {
                connectButton.Deselect();
            }
            // SaveConfigButton MouseOver Highlight
            if (saveConfigButton.Contains(e.X, e.Y))
            {
                saveConfigButton.Select();
            }
            else
            {
                saveConfigButton.Deselect();
            }
            // ResetButton MouseOver Highlight
            if (resetButton.Contains(e.X, e.Y))
            {
                resetButton.Select();
            }
            else
            {
                resetButton.Deselect();
            }
        }

        public override bool IsOpaque
        {
            get
            {
                return true;
            }
        }

        public override void Draw(float deltaTime)
        {
            game.window.Clear(new Color(0x11, 0x11, 0x11));

            // Draw texts
            game.window.Draw(title);
            game.window.Draw(playerNameLabel);
            game.window.Draw(hostnameLabel);
            game.window.Draw(portLabel);
            game.window.Draw(nametagColorLabel);
            game.window.Draw(playerColorLabel);

            // Draw inputs
            game.window.Draw(playerNameInput);
            game.window.Draw(hostnameInput);
            game.window.Draw(portInput);

            // Draw separator line
            game.window.Draw(separatorLine);

            // Draw sliders
            game.window.Draw(nametagColorSlider);
            game.window.Draw(playerColorSlider);

            // Draw buttons
            game.window.Draw(connectButton);
            game.window.Draw(saveConfigButton);
            game.window.Draw(resetButton);

            // Draw preview player entity
            game.window.Draw(playerPreview);
        }

        public override void HandleInput(float deltaTime)
        {
            if (Keyboard.IsKeyPressed(Keyboard.Key.Enter) && game.cd == 0)
            {
                switch(inputState)
                {
                    case InputState.PlayerName:
                        inputState = InputState.Hostname;
                        playerNameInput.FillColor = nametagColor;
                        hostnameInput.FillColor = highlightedColor;
                        Config.data.name = playerNameInput.DisplayedString.Trim();
                        break;
                    case InputState.Hostname:
                        inputState = InputState.Port;
                        hostnameInput.FillColor = defaultColor;
                        portInput.FillColor = highlightedColor;
                        Config.data.hostname = hostnameInput.DisplayedString.Trim();
                        break;
                    case InputState.Port:
                        inputState = InputState.None;
                        portInput.FillColor = defaultColor;
                        short.TryParse(portInput.DisplayedString.Trim(), out Config.data.port);
                        break;
                    case InputState.NametagColor:
                        inputState = InputState.None;
                        nametagColorSlider.Deselect();
                        break;
                    case InputState.PlayerColor:
                        inputState = InputState.None;
                        playerColorSlider.Deselect();
                        break;
                    case InputState.None:
                    default:
                        break;
                }
                game.cd += 0.15f;
            }
        }

        public override void Update(float deltaTime)
        {

        }

        private void UpdateNametagColor(int hue)
        {
            hue %= 360;
            nametagColor = ColorConversion.HSVtoRGB(hue, PlayerEntity.nametagColorSaturation, PlayerEntity.nametagColorValue);
            playerNameInput.FillColor = nametagColor;
            nametagSystemColor = ColorConversion.SFMLColorToSystemColor(nametagColor);
        }

        private void UpdatePlayerColor(int hue)
        {
            hue %= 360;
            playerPreview.FillColor = ColorConversion.HSVtoRGB(hue, PlayerEntity.playerColorSaturation, PlayerEntity.playerColorValue);
            playerPreview.OutlineColor = ColorConversion.HSVtoRGB(hue, PlayerEntity.playerOutlineColorSaturation, PlayerEntity.playerOutlineColorValue);
            playerSystemColor = ColorConversion.SFMLColorToSystemColor(playerPreview.FillColor);
        }
    }
}
