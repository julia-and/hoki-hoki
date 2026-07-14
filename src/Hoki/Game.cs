using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using FloatMath;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SpriteUtilities;
using Device = Microsoft.Xna.Framework.Graphics.GraphicsDevice;

namespace Hoki;

/// <summary>
/// Driver class for the game.
/// </summary>
public class Game : Microsoft.Xna.Framework.Game
{
    #region vars
    #region consts
    private const int
        highScoreCount = 3, //Number of high scores to save for each level
        buttonWidth = 130,  //Width of a normal button
        pButtonWidth = 180, //Width of a player select button
        renderSize = 1024,  //Resolution to render scenes at
                            //numLevels=26,		//TEMP/DEBUG: limit the number of levels that can be unlocked
        boxWidth = 140,     //Size of the left info boxes on the level selector
        boxMargin = 8;      //Spacing from the edges of the info boxes
    private const float
        frameLength = 0.016666f,
        blurRecover = 2,            //Rate of blur recovery, %/sec
        levelSwitchTime = 0.1f, //Time between level switches on the level select menu
        planetNameHeight = 20,  //Y-position of the planet names
        hitPenalty = 3,         //Time penalty for hitting a wall
        boxFadeDelay = 0,           //Delay before fading boxes in after selecting a level
        maxDeadTime = 2,            //Time until forced back to menu after death
        showControlTime = 4;        //Time to wait after a button is pressed to show the controls
    private const string
        highScoreFile = "scores",
        userFile = "user";
    public static readonly string
        MusicDir = "music" + Path.DirectorySeparatorChar;
    private readonly int[,]
        times;  //Time needed to get bronze, gold, silver medals
                //Control names
    private const string
        aName = "A",
        bName = "B",
        leftName = "L",
        rightName = "R",
        upName = "U",
        downName = "D",
        startName = "S",
        volumeName = "V",
        musicName = "M",
        fxName = "X",
        windowedName = "W",
        firstTimeName = "F",
        aaName = "N",
        highColorName = "T";
    #endregion

    #region window
    private int
        width,          //Size of the rendering area
        height;
    private Vector2
        centerPoint;    //Center of the rendering area
    private Viewport
        gameViewport;   //Letterboxed 4:3 viewport within the (resizable) backbuffer
    #endregion

    #region dx
    private GraphicsDeviceManager graphics;
    private Device device;
    private FontSystem fontSystem;
    private KeyboardState prevKeys;     //Previous keyboard state for edge-detecting AnyKeyDown/Up
    private MouseState prevMouse;
    public static float FXVolume = 1f;  //Sound effect volume, 0-1
    #endregion

    #region persistant vars
    private GameState
        gameState;      //State the game is currently in (ie intro, menu, main, etc.)
    private ArrayList
        update;         //List of Updateables
    private SpriteObject
        root;           //SpriteObject to which all others are ultimately children
    private Microsoft.Xna.Framework.Color
        background;     //Color the device should be cleared to
    private Hashtable
        levels,         //Loaded levels, keyed on map hashes
        themes;         //Loaded theme files, keyed on their hashes
    private float
        accumulator = 0,    //Accumulates frame times
        keyDelay = 0;       //Delay until key presses are accepted again
    private Player
        player;         //Profile currently in use
    private DataButton
        playerButton;   //Player button corresponding to the player currently in use
    private SpriteTexture
        blankTex;
    private Level
        playtestLevel;  //Set when a map path was passed on the command line
    private string
        tracePath;      //--trace <file>: dump heli positions for the editor's trace overlay
    private List<Vector3>
        tracePoints = new List<Vector3>();  //x,y = heli midpoint, z = rotation (radians)
    private Song
        currentSong;    //Song being played
    private bool
        windowed,       //Whether to be windowed or not
        firstTime,      //Whether this is the first time the game is being run
        musicOn,        //Whether to play music
        antialias;      //Whether to antialias stuff

    public static bool FXOn;    //Whether to play sound effects

    public event KeyEventHandler AnyKeyDown, AnyKeyUp;
    public event KeyPressEventHandler KeyPress;
    #endregion

    #region flecko
    private readonly int[,] pxLayout;   //Grid of 1s and 0s that describes which pixels are on and off
    private SpriteTexture whitePX;      //Plain white texture
    private SpriteObject fleckoLayer;   //SpriteObject that will hold all fleckopx
    private float fleckoTimeLeft;       //Number of seconds remaining before the game state switches
    private ArrayList pxList;           //List of all FallingFleckoPX in use
    private const float fleckoTime = 7.5f;//Total number of seconds the effect should last
    private const float fleckoDist = 1.5f;//Distance between px in the effect
    private const int fleckoStart = -100;   //Starting position for FallingFleckoPX
    private const int fleckoFadeIn = -50;   //Distance from layer origin px begin to fade in
    private const int fleckoFadeOut = 0;    //Distance from layer bottom px begin to fade out
    #endregion

    #region menu
    //Textures and surfaces
    private Texture2D
        previewTex;

    /// <summary>
    /// Rendering area size, kept for code that still thinks in WinForms terms
    /// </summary>
    public Point ClientSize
    {
        get { return new Point(width, height); }
    }

    private SizeTexture
        interfaceTex;   //All interface images

    private SpriteTexture
        buttonLeft,
        buttonMiddle,
        buttonRight,
        easyTex,
        trophyTex,
        perfectTex,
        starTex;

    private SpriteFontBase
        menuFontSmall,  //Font to use for selects, options
        menuFontLarge,  //Font to use for player details
        boneFont;       //BONESAW

    private SpriteObject
        menuLayer,
        iconLayer,
        lineLayer,
        textLayer,
        interfaceLayer,
        errMenuLayer,
        mapPreview,
        playerStar,
        playerBronze,
        playerSilver,
        playerGold,
        bonesaw;

    //Menu sublayers
    private Fader
        playerMenuLayer,
        newPlayerMenuLayer,
        mainMenuLayer,
        optionMenuLayer,
        creditsMenuLayer,
        levelMenuLayer,
        startMenuLayer,
        deleteMenuLayer,
        gameMenuLayer,
        ghostMenuLayer,
        viewMenuLayer,
        volumeMenuLayer,
        boxLayer,
        controlFader;

    //Menus
    private Menu
        mainMenu,
        optionMenu,
        creditsMenu,
        levelMenu,
        startMenu,
        playerMenu,
        newPlayerMenu,
        deleteMenu,
        gameMenu,
        ghostMenu,
        volumeMenu,
        viewMenu,
        errMenu,
        activeMenu,
        endMenu;

    //Menu positions
    private Vector2
        mainMenuPos,
        startMenuPos,
        playerMenuPos;

    private float levelSwitch;  //Time left before the player can move the focused level again

    //Level positions
    private Vector2[]
        levelPos;
    private SpriteObject[]
        markers;
    private Level[]
        gameLevels;         //Ordered list of levels used in the main game
    private int
        oldLevel,           //Last level the player was on
        currentLevel,       //Index of the level currently in focus
        lastUnlockedLevel;  //Index of the last unlocked level

    //Buttons
    private KeyButton
        startButton,
        newGameButton,
        loadLevelButton,
        optionsButton,
        creditsButton,
        exitButton,
        optionsOkButton,
        creditsOkButton,
        levelCancelButton,
        playerCancelButton,
        newPlayerButton,
        newPlayerOkButton,
        newPlayerCancelButton,
        signoutButton,
        deleteButton,
        deleteOkButton,
        deleteCancelButton,
        playButton,
        ghostButton,
        viewButton,
        playCancelButton,
        ghostOkButton,
        ghostCancelButton,
        viewCancelButton,
        volumeButton,
        errOkButton,
        musicButton,
        fxButton,
        difficultyButton,
        endKey;

    private Slider
        volumeSlider;

    //Info area
    private UIBox
        previewBox,
        playerTopBox,
        highscoreBox;

    private UIBoxTex
        boxTex;

    private float
        boxTime,            //Time until the boxes should be faded in
        controlTimeLeft;    //Time remaining until the controls are to be faded in

    private bool
        boxesIn = true; //True if the boxes are faded out but need to be faded in

    private SpriteText
        bestTimeText,
        highScoreText,
        errText,
        playerDetails,
        boneText;

    private int
        loadingLevel = -1;  //index of the level the loaded level the user is playing, or -1 if he isn't
    private const float
        boneWait = 300,
        boneLength = 5.568f;

    private float
        boneTimer;

    private bool
        boneStarted = false;

    private bool
        levelSelecting,     //Whether the user is in the level selection area
        playingMainGame;    //Whether the level being played is part of the main game

    //Other inputs
    private TextBox
        newPlayerInput,
        ghostText;

    //Star field
    private StarField stars;

    private DataButton[]
        levelButtons,
        ghostButtons;
    private Level[]
        userLevels;

    private string
        viewGhost;          //Ghost to watch

    private ArrayList
        players,            //All players
        playerButtons,
        errUpdate,          //Updateables for when an error is brought up
        menuList;           //All menus
    private Fader[]
        planetNames,        //Ordered list of SpriteObjects that draw the text of a planet's name
        planetNumbers;      //Numbers (starting from 1) to put next to planet names
    private const float
        nameFadeRate = 4;       //Rate of change in alpha of planet names, dec%/sec

    private SimpleSong
        menuSong,
        boneSong;

    #endregion

    #region main
    private Heli
        heli,               //Helicopter that the player controls
        ghost,              //Helicopter that the ghost controls (if any)
        focusedHeli;        //Helicopter for the camera to follow
    private KeyboardController
        controller;         //Controller that the local player is using
    private GhostController
        ghostController;    //Controller that moves the ghost
    private GhostRecorder
        recorder;           //Record ghosts
    private Map
        map;                //Constructed map to play on
    private Level
        playLevel;          //Level construct and use
    private bool
        started,
        finished,           //True once the level is complete
        dead,               //True if the player dies
        recording,          //Whether recording a ghost
        ghosting,           //Whether viewing a ghost
        paused;
    private float
        gameTime,           //Time spent in main mode
        deadTime;           //Count down after death until forced return
    private SpriteObject
        dataLayer;
    private SizeTexture
        heliSizeTex,
        gameUITex;
    private SpriteTexture
        heliTex,
        easyHeliTex;
    private SpriteTime
        timeGraphic;
    private SpriteObject[]
        healthIndicator;
    private Fader
        pauseMenuLayer;
    private Menu
        pauseMenu;
    private KeyButton
        restartButton,
        quitButton,
        continueButton;
    private ArrayList
        pauseUpdate;        //Stuff to update even while paused
    #endregion

    #endregion

    #region construct/initialize
    public Game(string[] args = null)
    {
        //Set up the window (fixed 640x480, like the original)
        graphics = new GraphicsDeviceManager(this);
        graphics.PreferredBackBufferWidth = 640;
        graphics.PreferredBackBufferHeight = 480;
        Window.Title = "Hoki";
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += onResize;
        IsMouseVisible = false;
        IsFixedTimeStep = false;    //The game keeps its own fixed-step accumulator

        //Store the size of the rendering area
        width = 640;
        height = 480;
        centerPoint = new Vector2(width / 2, height / 2);


        //Data structures to hold levels
        levels = new Hashtable(97); //Table of all levels
        themes = new Hashtable(17); //Table of all themes

        string map; //Temporarily hold loaded maps

        //Load the main game levels
        StreamReader listReader = new StreamReader(getStream("Hoki.data.levels"));      //Read the list file
        string[] levelList = listReader.ReadToEnd().Split('\n');
        listReader.Close();

        gameLevels = new Level[levelList.Length];
        for (int i = 0; i < levelList.Length; i++)
        {
            levelList[i] = levelList[i].Trim(); //The original chopped a trailing \r; git normalized the line endings
            StreamReader mapReader = new StreamReader(getStream("Hoki.data.maps." + levelList[i])); //Read the map file (all embedded)
            map = mapReader.ReadToEnd();
            mapReader.Close();

            string encrypted = StringCrypt.MD5(map);    //Encrypt the map to use as a key
            Level l = (Level)levels[encrypted];
            if (l == null)
            {   //If no entry existed, create one and add it to the hash table
                l = new Level(encrypted);
                levels.Add(encrypted, l);
            }
            l.GameMap = map;    //Set the level's map data
            int first = levelList[i].IndexOf('.');
            l.Name = levelList[i].Substring(first, levelList[i].LastIndexOf('.') - first);

            //Add it to the ordered list for the planets' use
            gameLevels[i] = l;
        }

        //Load user levels
        string levelDir = Path.Combine(AppContext.BaseDirectory, "levels");
        String[] userLevelFiles = Directory.Exists(levelDir) ? Directory.GetFiles(levelDir, "*.map") : new string[0];
        userLevels = new Level[userLevelFiles.Length];
        for (int i = 0; i < userLevelFiles.Length; i++)
        {
            StreamReader mapReader = new StreamReader(userLevelFiles[i]);   //Read the map file
            map = mapReader.ReadToEnd();
            mapReader.Close();

            string encrypted = StringCrypt.MD5(map);    //Encrypt the map to use as a key
            Level l = (Level)levels[encrypted];
            if (l == null)
            {   //If no entry existed, create one and add it to the hash table
                l = new Level(encrypted);
                levels.Add(encrypted, l);
            }
            l.GameMap = map;    //Set the level's map data
            int first = userLevelFiles[i].LastIndexOf(Path.DirectorySeparatorChar) + 1;
            l.Name = userLevelFiles[i].Substring(first, userLevelFiles[i].LastIndexOf('.') - first);

            //Add it to the user level list
            userLevels[i] = l;
        }

        //Load times for medals
        StreamReader medalReader = new StreamReader(getStream("Hoki.data.medals"));
        string[] levelMedals = medalReader.ReadToEnd().Split('\n');
        times = new int[levelMedals.Length, 3];
        for (int i = 0; i < levelMedals.Length; i++)
        {
            string[] medals = levelMedals[i].Split(',');
            for (int n = 0; n < 3; n++) times[i, n] = int.Parse(medals[n]);
        }

        //Load users (file may not exist on a fresh install)
        String[] lines = File.Exists(userFile) ? File.ReadAllText(userFile).Split('\n') : new string[0];

        Player currentPlayer = null;
        players = new ArrayList();
        String[] data;
        bool easy;
        foreach (String line in lines)
        {
            if (line.Length == 0) continue; //Ignore empty lines

            if (line[0] == '#')
            {
                if (currentPlayer != null)
                    currentPlayer.RefreshStatus(gameLevels, times);

                currentPlayer = new Player(line.Substring(1));
                players.Add(currentPlayer);
            }
            else
            {
                data = line.Split(':');
                if (line[0] == '>')
                    currentPlayer.Easy = (data[1][0] == '1' ? true : false);
                else
                {
                    if (data.Length > 3) easy = (int.Parse(data[3]) == 1); else easy = false;
                    currentPlayer.AddScore(data[0], new Score(currentPlayer.Name, int.Parse(data[1]), int.Parse(data[2]) == 1, easy));
                }
            }
        }

        //Refresh all players
        foreach (Player p in players) p.RefreshStatus(gameLevels, times);

        //Load scores (fall back to shipped defaults, then to nothing)
        string scorePath = File.Exists(highScoreFile) ? highScoreFile : (File.Exists("defaultscores.txt") ? "defaultscores.txt" : null);
        lines = scorePath != null ? File.ReadAllText(scorePath).Split('\n') : new string[0];
        Level currentLevel = null;
        int scoresTaken = 0;
        foreach (String line in lines)
        {
            if (line.Length == 0) continue; //Ignore blank lines

            if (line[0] == '>')
            {   //New level being declared

                if (levels.Contains(line.Substring(1)))
                    //Get the existing level from the hash table
                    currentLevel = (Level)levels[line.Substring(1)];
                else
                {
                    //Create a level on the given hash
                    currentLevel = new Level(line.Substring(1));
                    levels.Add(currentLevel.Hash, currentLevel);    //Add it to the table
                }
                scoresTaken = 0;                    //Haven't recorded any scores on this level yet
            }
            else
            {           //New score being recorded
                if (++scoresTaken > highScoreCount) continue;   //Only interested in a limited number of scores

                data = line.Split(':');
                if (data.Length > 3) easy = (int.Parse(data[3]) == 1); else easy = false;
                currentLevel.InsertScore(new Score(data[0], int.Parse(data[1]), (int.Parse(data[2]) == 1), easy));
            }
        }

        //Load ghosts
        string ghostDir = Path.Combine(AppContext.BaseDirectory, "ghosts");
        String[] ghostFiles = Directory.Exists(ghostDir) ? Directory.GetFiles(ghostDir, "*.ghost") : new string[0];
        foreach (String fileName in ghostFiles)
        {
            //Read the ghost's level hash
            StreamReader ghostReader = new StreamReader(fileName);
            String hash = ghostReader.ReadLine().Substring(1);  //Each file's first line should be #HASHCODE
            ghostReader.Close();

            //Add the ghost to the level
            Level level = (Level)levels[hash];
            if (level != null)
            {
                int lastSlash = fileName.LastIndexOf(Path.DirectorySeparatorChar) + 1;
                level.InsertGhost(fileName.Substring(lastSlash, fileName.LastIndexOf(".") - lastSlash));
            }
        }

        //Make a controller
        controller = new KeyboardController(this);
        controller.ControlDown += new ControlEventHandler(onControlDown);

        //Initialize the sound
        //Sound.InitSound(this);

        //Load controls and other preferences (defaults if no config file exists)
        Song.Volume = 100;
        musicOn = FXOn = true;
        windowed = true;
        if (File.Exists("config"))
        {
            StreamReader controlReader = new StreamReader("config");
            String controlLine;
            while ((controlLine = controlReader.ReadLine()) != null)
            {
                String[] controlMap = controlLine.Split(':');
                switch (controlMap[0])
                {
                    case aName: controller.SetKey(Hoki.Controls.A, (Keys)int.Parse(controlMap[1])); break;
                    case bName: controller.SetKey(Hoki.Controls.B, (Keys)int.Parse(controlMap[1])); break;
                    case leftName: controller.SetKey(Hoki.Controls.Left, (Keys)int.Parse(controlMap[1])); break;
                    case rightName: controller.SetKey(Hoki.Controls.Right, (Keys)int.Parse(controlMap[1])); break;
                    case downName: controller.SetKey(Hoki.Controls.Down, (Keys)int.Parse(controlMap[1])); break;
                    case upName: controller.SetKey(Hoki.Controls.Up, (Keys)int.Parse(controlMap[1])); break;
                    case startName: controller.SetKey(Hoki.Controls.Start, (Keys)int.Parse(controlMap[1])); break;
                    case volumeName: Song.Volume = float.Parse(controlMap[1]); break;
                    case musicName: musicOn = controlMap[1][0] == '1' ? true : false; break;
                    case windowedName: windowed = controlMap[1][0] == '1' ? true : false; break;
                    case firstTimeName: firstTime = controlMap[1][0] == '1' ? true : false; break;
                    case fxName: FXOn = controlMap[1][0] == '1' ? true : false; break;
                    case aaName: antialias = controlMap[1][0] == '1' ? true : false; break;
                        //highColorName obsolete: textures always load 32bpp now
                }
            }
            controlReader.Close();
        }

        //Set the layout for the flecko.net intro pixels
        pxLayout = new int[,] { {
             1,1,1,0,1,0,0,0,1,1,1,0,1,1,1,0,1,0,1,0,1,1,1,0,0,0,1,0,0,1,0,1,1,1,0,1,1,1}, {
             1,0,0,0,1,0,0,0,1,0,0,0,1,0,0,0,1,0,1,0,1,0,1,0,0,0,1,1,0,1,0,1,0,0,0,0,1,0}, {
             1,1,0,0,1,0,0,0,1,1,1,0,1,0,0,0,1,1,0,0,1,0,1,0,0,0,1,0,1,1,0,1,1,1,0,0,1,0}, {
             1,0,0,0,1,0,0,0,1,0,0,0,1,0,0,0,1,0,1,0,1,0,1,0,0,0,1,0,0,1,0,1,0,0,0,0,1,0}, {
             1,0,0,0,1,1,1,0,1,1,1,0,1,1,1,0,1,0,1,0,1,1,1,0,1,0,1,0,0,1,0,1,1,1,0,0,1,0}
        };

        //Create an update list
        update = new ArrayList();

        //Capture keydown
        AnyKeyDown += new KeyEventHandler(onKeyDown);
        KeyPress += new KeyPressEventHandler((s, e) => { });    //Never null, like the AnyKey events
        Window.TextInput += (s, e) => KeyPress(this, new KeyPressEventArgs(e.Character));

        //Playtest: a .map path as the first argument loads that map directly
        if (args != null && args.Length >= 1 && File.Exists(args[0]))
        {
            string mapText = File.ReadAllText(args[0]);
            playtestLevel = new Level(StringCrypt.MD5(mapText));
            playtestLevel.GameMap = mapText;
            playtestLevel.Name = Path.GetFileNameWithoutExtension(args[0]);

            //--trace <file>: record heli positions for the editor's trace overlay
            for (int i = 1; i < args.Length - 1; i++)
                if (args[i] == "--trace") tracePath = args[i + 1];

            //--invincible: playtest without dying; onFinish skips score recording while set
            Heli.Invincible = Array.IndexOf(args, "--invincible") > 0;
        }

        //Load sound effects (SoundEffect.Play spawns a new voice per call, so one instance per effect suffices)
        FXVolume = Song.Volume / 100f;
        Explosion.Sound = loadSfx("Hoki.fx.explosion.wav");
        Heli.HitSound = loadSfx("Hoki.fx.hit.wav");
        Heli.HealSound = loadSfx("Hoki.fx.heal.wav");
        Spring.Sound = loadSfx("Hoki.fx.spring.wav");
    }

    /// <summary>
    /// Loads one embedded sound effect; a failed load (odd wav header, no audio device) means that
    /// effect stays silent instead of crashing the game.
    /// </summary>
    private SoundEffect loadSfx(string resourcePath)
    {
        try
        {
            return SoundEffect.FromStream(getStream(resourcePath));
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("sfx load failed: " + resourcePath + " — " + e.Message);
            return null;
        }
    }

    /// <summary>
    /// Plays a sound effect if effects are on and it actually loaded. All sfx playback routes through here.
    /// </summary>
    public static void PlaySfx(SoundEffect sound)
    {
        if (FXOn && sound != null) sound.Play(FXVolume, 0, 0);
    }

    protected override void LoadContent()
    {
        device = GraphicsDevice;

        //Shared renderer state: ortho pixel camera + blend/sampler states applied per draw
        SpriteObject.SetupCamera(device, width, height);

        //Initialize fonts (original Century Gothic Bold replaced by Jost Bold, an OFL look-alike)
        fontSystem = new FontSystem();
        fontSystem.AddFont(getStream("Hoki.resources.fonts.Jost.Jost-Bold.ttf"));
        menuFontSmall = fontSystem.GetFont(13);
        menuFontLarge = fontSystem.GetFont(15);
        boneFont = fontSystem.GetFont(72);

        //Load persistant textures
        string path = "Hoki.textures.menu.";
        blankTex = loadTexture(loadSizeTexture(path + "blank.png"), path + "blank.dat");

        SizeTexture buttonTex = loadSizeTexture(path + "buttons.png");
        buttonLeft = loadTexture(buttonTex, path + "buttonleft.dat");
        buttonMiddle = loadTexture(buttonTex, path + "buttonmiddle.dat");
        buttonRight = loadTexture(buttonTex, path + "buttonright.dat");

        //Make the root SpriteObject
        root = new SpriteObject(device, null);

        //Initialize the game
        initializeGame();
    }

    private void initializeGame()
    {
        //Playtest mode (map path passed on the command line, e.g. from the level editor):
        //skip the intro and jump straight into the map with a throwaway profile.
        if (playtestLevel != null)
        {
            player = new Player("playtest");    //Not added to players, so it is never written to disk
            play(playtestLevel);
            return;
        }

        //Set the game state to the flecko.net intro
        setGameState(GameState.Flecko);
    }
    #endregion

    #region paint
    protected override void Update(GameTime xnaTime)
    {
        base.Update(xnaTime);

        pollInput();

        if (boneStarted) boneText.Visible = !boneText.Visible;

        //Updates
        float et = (float)xnaTime.ElapsedGameTime.TotalSeconds;
        if (et < 0.1f)
        {   //Enforce a 10fps minimum/skip excessively delayed frames
            //If applicable, make sure the ghost is started
            if (ghostController != null && !ghostController.Started)
                ghostController.Start();

            accumulator += et;
            while (accumulator > frameLength)
            {
                accumulator -= frameLength;
                doUpdates(frameLength);
            }
        }

        //GameState updates
        keyDelay -= et;
        switch (gameState)
        {
            case GameState.Main:
                if (paused) break;
                timeGraphic.setTime(Score.GetTimeString((int)(gameTime * 1000)));
                if (started && !finished)
                {
                    gameTime += et;
                    if (tracePath != null) tracePoints.Add(new Vector3(focusedHeli.Midpoint, focusedHeli.Rotation));
                }
                else if (dead)
                {
                    deadTime -= et;
                    if (deadTime < 0 && !paused)
                    {
                        //Explosion has played out; offer restart/quit instead of forcing back to the level select
                        pauseMenu.RemoveElement(continueButton);
                        pauseMenu.Y = (height - pauseMenu.Height) / 2;
                        showPauseMenu();
                    }
                }

                //Update health display
                for (int i = 0; i < healthIndicator.Length; i++)
                    healthIndicator[i].Frame = (i < heli.Health ? 1 : 0);

                map.ScrollTo(focusedHeli.Midpoint - centerPoint);
                break;
            case GameState.Flecko:
                fleckoTimeLeft -= et;
                if (fleckoTimeLeft < 0) setGameState(GameState.Menu);
                break;
            case GameState.Menu:
                if (paused)
                {
                    foreach (Updateable u in errUpdate) u.Update(et);
                    break;
                }
                levelSwitch -= et;
                boxTime -= et;
                if (boxTime < 0 && !boxesIn && mapPreview != null)
                {
                    boxesIn = true;
                    boxLayer.FadeTarget = TransformedObject.MaxAlpha;
                    boxLayer.Add(mapPreview);
                }

                controlTimeLeft -= et;
                if (controlTimeLeft < 0 && controlFader.FadeTarget == 0) controlFader.FadeTarget = TransformedObject.MaxAlpha;

                break;
        }
    }

    /// <summary>
    /// Resizes the backbuffer to the window and letterboxes a 4:3 viewport into it.
    /// The scene keeps its logical 640x480 projection and rasterizes at native resolution.
    /// </summary>
    private void onResize(object sender, EventArgs e)
    {
        int w = Window.ClientBounds.Width, h = Window.ClientBounds.Height;
        if (w == 0 || h == 0) return;   //Minimized
        if (w != graphics.PreferredBackBufferWidth || h != graphics.PreferredBackBufferHeight)
        {
            graphics.PreferredBackBufferWidth = w;
            graphics.PreferredBackBufferHeight = h;
            graphics.ApplyChanges();
        }
        float scale = Math.Min(w / (float)width, h / (float)height);
        int vw = (int)(width * scale), vh = (int)(height * scale);
        gameViewport = new Viewport((w - vw) / 2, (h - vh) / 2, vw, vh);
    }

    protected override void Draw(GameTime xnaTime)
    {
        device.Clear(background);   //Clears the whole backbuffer, letterbox bars included

        if (gameViewport.Width > 0) device.Viewport = gameViewport;

        //Perform the drawing operations
        root.Draw();

        base.Draw(xnaTime);
    }

    /// <summary>
    /// Polls the keyboard/mouse and raises the WinForms-era events the input plumbing consumes.
    /// </summary>
    private void pollInput()
    {
        KeyboardState keys = Keyboard.GetState();
        MouseState mouse = Mouse.GetState();

        foreach (Keys k in keys.GetPressedKeys())
            if (!prevKeys.IsKeyDown(k) && keyDelay < 0 && AnyKeyDown != null)
                AnyKeyDown(this, new KeyEventArgs(k));

        foreach (Keys k in prevKeys.GetPressedKeys())
            if (!keys.IsKeyDown(k) && AnyKeyUp != null)
                AnyKeyUp(this, new KeyEventArgs(k));

        //End the flecko px effect on left click (was OnMouseDown)
        if (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released && gameState == GameState.Flecko)
            fleckoTimeLeft = 0;

        prevKeys = keys;
        prevMouse = mouse;
    }

    /// <summary>
    /// Update() everything in the update arraylist
    /// </summary>
    /// <param name="et"></param>
    private void doUpdates(float et)
    {
        if (gameState == GameState.Main) foreach (Updateable u in pauseUpdate) u.Update(et);    //Update the pause menu
        if (!paused) for (int i = 0; i < update.Count; i++) ((Updateable)update[i]).Update(et);

        //Bonesaw is a rare breed, a cross between a sloth and a beautiful lion
        if (gameState == GameState.Menu)
        {
            if (!startMenu.Locked || boneStarted)
                boneTimer -= et;
            else
            {
                if (boneText.Visible) boneText.Visible = bonesaw.Visible = false;
                boneTimer = boneWait;
            }

            if (boneTimer < -boneLength)
            {
                boneText.Visible = false;
                bonesaw.Visible = false;

                if (boneStarted)
                {
                    boneStarted = false;
                    currentSong.Stop();
                    currentSong = menuSong;
                    if (musicOn) currentSong.Play();
                }
            }
            else if (boneTimer < 0)
            {
                bonesaw.Visible = true;

                if (!boneStarted)
                {
                    boneStarted = true;

                    currentSong.Stop();
                    currentSong = boneSong;
                    currentSong.Play();
                }
            }
        }
    }
    #endregion

    #region gamestate
    #region switch
    private void setGameState(GameState g)
    {
        //End the old game state
        switch (gameState)
        {
            case GameState.Flecko:
                fleckoEnd();
                break;
            case GameState.Menu:
                menuEnd();
                break;
            case GameState.Main:
                mainEnd();
                break;
        }

        //Set the new game state
        gameState = g;

        //Begin the game state
        switch (g)
        {
            case GameState.Flecko:
                fleckoBegin();
                break;
            case GameState.Menu:
                menuBegin();
                break;
            case GameState.Main:
                mainBegin();
                break;
        }
    }
    #endregion

    #region flecko
    /// <summary>
    /// Called to initialize the flecko px intro
    /// </summary>
    private void fleckoBegin()
    {
        //Set the stage color
        background = Microsoft.Xna.Framework.Color.Black;

        //Set the timer
        fleckoTimeLeft = fleckoTime;

        //Initialize the layer
        fleckoLayer = new SpriteObject(device, null);

        //Create FleckoPX texture
        whitePX = loadTexture(loadSizeTexture("Hoki.textures.flecko.whitepx.bmp"), "Hoki.textures.flecko.whitepx.dat");

        //Create FleckoPX sprites
        float pxSpace = whitePX.Width * fleckoDist;

        //Calculate size and layer position
        float pxHeight = pxLayout.GetLength(0) * pxSpace; //Save height for later use
        fleckoLayer.X = (int)((width - pxLayout.GetLength(1) * pxSpace) / 2.0);
        fleckoLayer.Y = (int)((height - pxHeight) / 2.0);

        //Variables for px initialization
        int rows = pxLayout.GetLength(0),
            cols = pxLayout.GetLength(1);

        int[] fallingList;

        pxList = new ArrayList();

        FallingFleckoPX.Initialize(fleckoFadeIn, (int)pxHeight + fleckoFadeOut);
        FallingFleckoPX.OnFleckoPXCreate += new FleckoPXHandler(OnFleckoPXCreate);
        FallingFleckoPX px;
        for (int i = 0; i < cols; i++)
        {
            //Make a list of pixels to be created by this FallingFleckoPX
            fallingList = new int[rows];
            for (int n = 0; n < rows; n++) fallingList[n] = pxLayout[n, i];

            //Create the px and set its properties
            px = new FallingFleckoPX(device, whitePX, fleckoDist, fallingList, Rand.NextFloat());
            px.X = i * pxSpace;
            px.Y = fleckoStart;
            update.Add(px);
            fleckoLayer.Add(px);
            pxList.Add(px);
        }

        //Add layers to root
        root.Add(fleckoLayer);


        // device.RenderState.MultiSampleAntiAlias=antialias;
    }

    /// <summary>
    /// Called at the end of the flecko px intro
    /// </summary>
    private void fleckoEnd()
    {
        FallingFleckoPX.OnFleckoPXCreate -= new FleckoPXHandler(OnFleckoPXCreate);

        foreach (FleckoPX px in pxList) px.Unhook();
        pxList = null;

        fleckoLayer.Clear();

        root.Remove(fleckoLayer);   //Remove the flecko layer from the root

        fleckoLayer = null;
        whitePX = null;
        update.Clear();             //Empty the updateable list

        GC.Collect();
    }

    /// <summary>
    /// Called when a new FleckoPX is created (adds it to the update list and layer
    /// </summary>
    private void OnFleckoPXCreate(object sender, FleckoPXEventArgs e)
    {
        update.Add(e.created);
        fleckoLayer.Add(e.created);
        pxList.Add(e.created);
    }
    #endregion

    #region menu

    private void menuBegin()
    {
        #region stage settings
        //Set filters
        Renderer.Filter = TextureFilter.Point;

        //device.SamplerState[0].MinFilter=TextureFilter.Point;
        //device.SamplerState[0].MagFilter=TextureFilter.Point;

        //Turn off antialiasing


        //Use a colored bg
        background = Microsoft.Xna.Framework.Color.Black;

        //No ghost by default
        viewGhost = null;
        #endregion

        #region textures and headers
        //Load in the interface texture image
        interfaceTex = loadSizeTexture("Hoki.textures.menu.interface.png");

        //Load interface textures
        string path = "Hoki.textures.menu.";
        easyTex = loadTexture(interfaceTex, path + "easy.dat");
        trophyTex = loadTexture(interfaceTex, path + "trophy.dat");
        perfectTex = loadTexture(interfaceTex, path + "star.dat");

        SpriteTexture
            markerTex = loadTexture(interfaceTex, path + "marker.dat"),
            sliderTex = loadTexture(interfaceTex, path + "slider.dat"),
            barSide = loadTexture(interfaceTex, path + "barside.dat"),
            barMiddle = loadTexture(interfaceTex, path + "barmiddle.dat"),
            aTex = loadTexture(interfaceTex, path + "a.dat"),
            bTex = loadTexture(interfaceTex, path + "b.dat"),
            arrowTex = loadTexture(interfaceTex, path + "arrow.dat"),
            pauseTex = loadTexture(interfaceTex, path + "pause.dat");

        boxTex = new UIBoxTex(this, interfaceTex, "Hoki.textures.menu.box");

        //Make image texts
        path = "Hoki.textures.menu.text.";
        SpriteObject
            startText = new SpriteObject(device, loadTexture(interfaceTex, path + "starttext.dat")),
            hokiText = new SpriteObject(device, loadTexture(interfaceTex, path + "hokihoki.dat")),
            optionsText = new SpriteObject(device, loadTexture(interfaceTex, path + "options.dat")),
            creditsText = new SpriteObject(device, loadTexture(interfaceTex, path + "credits.dat")),
            playerText = new SpriteObject(device, loadTexture(interfaceTex, path + "playerselect.dat")),
            newPlayerText = new SpriteObject(device, loadTexture(interfaceTex, path + "newplayer.dat")),
            deleteText = new SpriteObject(device, loadTexture(interfaceTex, path + "delete.dat")),
            volumeText = new SpriteObject(device, loadTexture(interfaceTex, path + "volume.dat"));

        //Planet name texts
        planetNames = new Fader[] {
            new Fader(device,loadTexture(interfaceTex,path+"training.dat")),
            new Fader(device,loadTexture(interfaceTex,path+"desert.dat")),
            new Fader(device,loadTexture(interfaceTex,path+"castle.dat")),
            new Fader(device,loadTexture(interfaceTex,path+"lava.dat")),
            new Fader(device,loadTexture(interfaceTex,path+"parquet.dat")),
            new Fader(device,loadTexture(interfaceTex,path+"sumie.dat")),
            new Fader(device,loadTexture(interfaceTex,path+"bamboo.dat")),
            new Fader(device,loadTexture(interfaceTex,path+"cannon.dat")),
        };

        planetNumbers = new Fader[] {
            new Fader(device,loadTexture(interfaceTex,path+"1.dat")),
            new Fader(device,loadTexture(interfaceTex,path+"2.dat")),
            new Fader(device,loadTexture(interfaceTex,path+"3.dat")),
            new Fader(device,loadTexture(interfaceTex,path+"4.dat")),
            new Fader(device,loadTexture(interfaceTex,path+"5.dat"))
        };

        //Starfield textures
        starTex = loadTexture(loadSizeTexture("Hoki.textures.space.starfield.star.png"), "Hoki.textures.space.starfield.star.dat");
        #endregion

        #region layering
        //Create starfield
        stars = new StarField(device, starTex, width, height, 100);
        stars.StarRotation = FMath.PI / 2;
        root.Add(stars);
        update.Add(stars);

        //Create layers
        SpriteObject
            markerLayer = new SpriteObject(device, null);
        menuLayer = new SpriteObject(device, null);
        iconLayer = new SpriteObject(device, null);
        textLayer = new SpriteObject(device, null);
        lineLayer = new SpriteObject(device, null);
        interfaceLayer = new SpriteObject(device, null);

        stars.AddObject(menuLayer);
        stars.AddObject(lineLayer);
        stars.AddObject(markerLayer);
        stars.AddObject(iconLayer);

        root.Add(textLayer);
        root.Add(interfaceLayer);

        //Menu sublayers
        startMenuLayer = new Fader(device, null);
        startMenuLayer.FadeTarget = 255;
        update.Add(startMenuLayer);
        menuLayer.Add(startMenuLayer);

        playerMenuLayer = new Fader(device, null);
        playerMenuLayer.FadeTarget = 0;
        update.Add(playerMenuLayer);
        menuLayer.Add(playerMenuLayer);

        mainMenuLayer = new Fader(device, null);
        update.Add(mainMenuLayer);
        menuLayer.Add(mainMenuLayer);

        optionMenuLayer = new Fader(device, null);
        optionMenuLayer.FadeTarget = 0;
        update.Add(optionMenuLayer);
        menuLayer.Add(optionMenuLayer);

        volumeMenuLayer = new Fader(device, null);
        volumeMenuLayer.FadeTarget = 0;
        update.Add(volumeMenuLayer);
        menuLayer.Add(volumeMenuLayer);

        deleteMenuLayer = new Fader(device, null);
        deleteMenuLayer.FadeTarget = 0;
        update.Add(deleteMenuLayer);
        menuLayer.Add(deleteMenuLayer);

        gameMenuLayer = new Fader(device, null);
        gameMenuLayer.FadeTarget = 0;
        update.Add(gameMenuLayer);
        interfaceLayer.Add(gameMenuLayer);      //Add directly to root layer

        ghostMenuLayer = new Fader(device, null);
        ghostMenuLayer.FadeTarget = 0;
        update.Add(ghostMenuLayer);
        interfaceLayer.Add(ghostMenuLayer);

        viewMenuLayer = new Fader(device, null);
        viewMenuLayer.FadeTarget = 0;
        update.Add(viewMenuLayer);
        interfaceLayer.Add(viewMenuLayer);
        #endregion

        #region boxes
        boxLayer = new Fader(device, null);
        boxLayer.FadeTarget = 0;
        update.Add(boxLayer);
        root.Add(boxLayer);

        previewBox = new UIBox(device, boxTex, boxWidth, boxWidth);
        previewBox.X = previewBox.Y = boxMargin;
        boxLayer.Add(previewBox);

        playerTopBox = new UIBox(device, boxTex, boxWidth, 20);
        playerTopBox.X = previewBox.X;
        playerTopBox.Y = previewBox.Y + previewBox.Height + boxMargin;
        boxLayer.Add(playerTopBox);

        highscoreBox = new UIBox(device, boxTex, boxWidth, 60);
        highscoreBox.X = playerTopBox.X;
        highscoreBox.Y = playerTopBox.Y + playerTopBox.Height + boxMargin;
        boxLayer.Add(highscoreBox);

        //Make a SpriteText to list the player's best time
        bestTimeText = new SpriteText(device, menuFontSmall, boxWidth, 100);
        bestTimeText.X = 3;
        playerTopBox.Add(bestTimeText);

        //Make a SpriteText to list high scores
        highScoreText = new SpriteText(device, menuFontSmall, boxWidth, 100);
        highScoreText.X = 3;
        highScoreText.Y = 1;
        highscoreBox.Add(highScoreText);

        #endregion

        #region menus

        menuList = new ArrayList();

        #region startmenu
        //START MENU
        startMenuPos = new Vector2(0, 0);   //Determine the menu screen's position in space

        startText.X = startMenuPos.X + (width - startText.Width) / 2;
        startText.Y = startMenuPos.Y + (height - startText.Height) / 2 - 40;
        startMenuLayer.Add(startText);

        startMenu = new Menu(device, controller);
        startMenu.X = startMenuPos.X + (width - buttonWidth) / 2;
        startMenu.Y = startText.Y + startText.Height + 40;
        startMenuLayer.Add(startMenu);
        update.Add(startMenu);
        menuList.Add(startMenu);

        startButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
        startButton.Text = "Start";
        startButton.Press += new EventHandler(onGameStart);
        startMenu.AddElement(startButton);

        exitButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
        exitButton.Text = "Exit";
        exitButton.Press += new EventHandler(onExit);
        startMenu.AddElement(exitButton);

        if (firstTime)
        {
            SpriteText quickInstructions = new SpriteText(device, menuFontSmall, 350, 100);
            quickInstructions.X = startText.X - (quickInstructions.Width - startText.Width);
            quickInstructions.Y = startMenu.Y + startMenu.Height + 50;
            quickInstructions.Tint = Microsoft.Xna.Framework.Color.White;
            quickInstructions.Format = FontDrawFlags.Center;
            quickInstructions.Text = "Use the directional keys to navigate the menu\nPress button 1 to select, button 2 to cancel\nIn-game, use buttons 1 and 2 to go faster";
            menuLayer.Add(quickInstructions);

            firstTime = false;
            saveConfig();
        }
        #endregion

        #region player select
        //PLAYER SELECT MENU
        playerMenuPos = startMenuPos + new Vector2(0, height);

        playerText.X = playerMenuPos.X + 20;
        playerText.Y = playerMenuPos.Y + 20;
        playerMenuLayer.Add(playerText);

        playerMenu = new Menu(device, controller);
        playerMenu.Lock();
        playerMenu.X = playerText.X;
        playerMenu.Y = playerText.Y + playerText.Height + 10;
        playerMenuLayer.Add(playerMenu);
        update.Add(playerMenu);
        menuList.Add(playerMenu);

        playerButtons = new ArrayList(players.Count);
        for (int i = 0; i < players.Count; i++)
        {
            //Make a new button for each player
            DataButton current = new DataButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, pButtonWidth);
            playerButtons.Add(current);

            //Associate the button with a player
            Player p = (Player)players[i];
            current.Text = p.Name;
            current.Data = p;

            //Add the button to the menu and hook its press event
            playerMenu.AddElement(current);
            current.Press += new EventHandler(onPlayerSelect);
            current.Over += new EventHandler(onPlayerOver);
            current.Out += new EventHandler(onPlayerOut);
        }

        //Player select objects
        playerDetails = new SpriteText(device, menuFontLarge, 200, 200);
        playerStar = new SpriteObject(device, perfectTex);
        playerBronze = new SpriteObject(device, trophyTex);
        playerSilver = new SpriteObject(device, trophyTex);
        playerGold = new SpriteObject(device, trophyTex);

        playerStar.Visible = false;
        playerBronze.Visible = false;
        playerSilver.Visible = false;
        playerGold.Visible = false;

        playerDetails.Tint = Microsoft.Xna.Framework.Color.White;

        playerSilver.Frame = 1;
        playerGold.Frame = 2;

        playerMenu.Add(playerDetails);
        playerMenu.Add(playerStar);
        playerMenu.Add(playerBronze);
        playerMenu.Add(playerSilver);
        playerMenu.Add(playerGold);

        //Button to create a new player
        newPlayerButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, pButtonWidth);
        newPlayerButton.Text = "New Player";
        newPlayerButton.Press += new EventHandler(onNewPlayer);
        playerMenu.AddElement(newPlayerButton);

        //Button to go back to the start menu
        playerCancelButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, pButtonWidth);
        playerCancelButton.Text = "Cancel";
        playerCancelButton.Press += new EventHandler(onPlayerCancel);
        playerMenu.AddElement(playerCancelButton, true);
        #endregion

        #region new player
        //NEW PLAYER MENU
        newPlayerMenuLayer = new Fader(device, null);
        newPlayerMenuLayer.FadeTarget = 0;
        menuLayer.Add(newPlayerMenuLayer);
        update.Add(newPlayerMenuLayer);

        newPlayerText.X = playerText.X + Math.Max(playerMenu.Width, playerText.Width) + 20;
        newPlayerText.Y = playerText.Y;
        newPlayerMenuLayer.Add(newPlayerText);

        newPlayerMenu = new Menu(device, controller);
        newPlayerMenu.X = newPlayerText.X;
        newPlayerMenu.Y = newPlayerText.Y + newPlayerText.Height + 10;
        newPlayerMenu.Lock();
        update.Add(newPlayerMenu);
        newPlayerMenuLayer.Add(newPlayerMenu);
        menuList.Add(newPlayerMenu);

        newPlayerInput = new TextBox(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, pButtonWidth, 100, this, newPlayerMenu);
        newPlayerInput.MaxWidth = 75;
        newPlayerMenu.AddElement(newPlayerInput);

        newPlayerOkButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, pButtonWidth);
        newPlayerOkButton.Text = "Ok";
        newPlayerOkButton.Press += new EventHandler(onNewPlayerOk);
        newPlayerMenu.AddElement(newPlayerOkButton);

        newPlayerCancelButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, pButtonWidth);
        newPlayerCancelButton.Text = "Cancel";
        newPlayerCancelButton.Press += new EventHandler(onNewPlayerCancel);
        newPlayerMenu.AddElement(newPlayerCancelButton);
        #endregion

        #region mainmenu
        //MAIN MENU
        mainMenuPos = playerMenuPos + new Vector2(0, height);

        hokiText.X = mainMenuPos.X + 20;
        hokiText.Y = mainMenuPos.Y + 20;
        mainMenuLayer.Add(hokiText);

        mainMenu = new Menu(device, controller);
        mainMenu.X = hokiText.X;
        mainMenu.Y = hokiText.Y + hokiText.Height + 10;
        mainMenu.Lock();
        mainMenuLayer.Add(mainMenu);
        update.Add(mainMenu);
        menuList.Add(mainMenu);

        newGameButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
        newGameButton.Text = "Play Game";
        newGameButton.Press += new EventHandler(onNewGame);
        mainMenu.AddElement(newGameButton);

        loadLevelButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
        loadLevelButton.Text = "Load Level";
        loadLevelButton.Press += new EventHandler(onLoadLevel);
        mainMenu.AddElement(loadLevelButton);

        optionsButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
        optionsButton.Text = "Options";
        optionsButton.Press += new EventHandler(onOptions);
        mainMenu.AddElement(optionsButton);

        creditsButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
        creditsButton.Text = "Credits";
        creditsButton.Press += new EventHandler(onCredits);
        mainMenu.AddElement(creditsButton);

        signoutButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
        signoutButton.Text = "Sign out";
        signoutButton.Press += new EventHandler(onSignOut);
        mainMenu.AddElement(signoutButton, true);
        #endregion

        #region options
        //OPTIONS MENU
        optionsText.X = hokiText.X + Math.Max(hokiText.Width, buttonWidth) + 20;
        optionsText.Y = hokiText.Y;
        optionMenuLayer.Add(optionsText);

        optionMenu = new Menu(device, controller);
        optionMenu.X = optionsText.X;
        optionMenu.Y = mainMenu.Y;
        optionMenu.Lock();
        optionMenuLayer.Add(optionMenu);
        update.Add(optionMenu);
        menuList.Add(optionMenu);

        deleteButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
        deleteButton.Text = "Delete Player";
        deleteButton.Press += new EventHandler(onDelete);
        optionMenu.AddElement(deleteButton, false);

        volumeButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
        volumeButton.Text = "Adjust Volume";
        volumeButton.Press += new EventHandler(onVolume);
        optionMenu.AddElement(volumeButton, false);

        musicButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
        musicButton.Text = "Music: " + (musicOn ? "on" : "off");
        musicButton.Press += new EventHandler(onMusicToggle);
        optionMenu.AddElement(musicButton);

        fxButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
        fxButton.Text = "FX: " + (FXOn ? "on" : "off");
        fxButton.Press += new EventHandler(onFXToggle);
        optionMenu.AddElement(fxButton);

        difficultyButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
        difficultyButton.Press += new EventHandler(onDifficultyToggle);
        if (player != null) difficultyButton.Text = "Difficulty: " + (player.Easy ? "easy" : "normal");
        optionMenu.AddElement(difficultyButton);

        optionsOkButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
        optionsOkButton.Text = "Ok";
        optionsOkButton.Press += new EventHandler(onOptionsOk);
        optionMenu.AddElement(optionsOkButton, true);
        #endregion

        #region volume
        volumeText.X = optionsText.X + Math.Max(optionsText.Width, buttonWidth) + 20;
        volumeText.Y = optionsText.Y;
        volumeMenuLayer.Add(volumeText);

        volumeMenu = new Menu(device, controller);
        volumeMenu.X = volumeText.X;
        volumeMenu.Y = mainMenu.Y;
        volumeMenu.Lock();
        volumeMenuLayer.Add(volumeMenu);
        update.Add(volumeMenu);
        menuList.Add(volumeMenu);

        volumeSlider = new Slider(device, barSide, barMiddle, barSide, sliderTex, buttonWidth, buttonMiddle.Height);
        volumeSlider.Value = Song.Volume / 100.0f;
        volumeSlider.Change += new EventHandler(onVolumeChange);
        volumeSlider.Press += new EventHandler(onVolumeOk);
        volumeMenu.AddElement(volumeSlider, true);

        #endregion

        #region credits
        //CREDITS MENU
        creditsMenuLayer = new Fader(device, null);
        update.Add(creditsMenuLayer);
        menuLayer.Add(creditsMenuLayer);

        creditsText.X = hokiText.X + Math.Max(hokiText.Width, buttonWidth) + 20;
        creditsText.Y = hokiText.Y;
        creditsMenuLayer.Add(creditsText);

        SpriteText credits = new SpriteText(device, menuFontSmall, 400, 250);
        credits.Tint = Microsoft.Xna.Framework.Color.White;
        credits.Text = "HOKI HOKI\n\nDeveloped by Max Abernethy\nMusic by 409 (www.four09.org)\nCreated with FMOD and Ogg Vorbis\n\nTextures by:\n  John Hunter, http://www.zipcon.net/~john/\n  Citrus Moon, http://citrusmoon.typepad.com/\n  http://astronomy.swin.edu.au/~pbourke/texture/\n  C.X. (made the little blue tiles)\n\nFlecko.net 2005";
        credits.X = creditsText.X;
        credits.Y = mainMenu.Y;
        creditsMenuLayer.Add(credits);

        creditsMenu = new Menu(device, controller);
        creditsMenu.X = credits.X;
        creditsMenu.Y = credits.Y + credits.Height + 10;
        creditsMenu.Lock();
        creditsMenuLayer.Add(creditsMenu);
        update.Add(creditsMenu);
        menuList.Add(creditsMenu);

        creditsOkButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
        creditsOkButton.Text = "Ok";
        creditsOkButton.Press += new EventHandler(onCreditsOk);
        creditsMenu.AddElement(creditsOkButton, true);
        #endregion

        #region delete
        deleteText.X = optionMenu.X + Math.Max(optionsText.Width, buttonWidth) + 20;
        deleteText.Y = creditsText.Y;
        deleteMenuLayer.Add(deleteText);

        SpriteText confirmText = new SpriteText(device, menuFontSmall, 300, 20);
        confirmText.Text = "This will delete all of your progress.";
        confirmText.X = deleteText.X;
        confirmText.Y = deleteText.Y + deleteText.Height + 10;
        confirmText.Tint = Microsoft.Xna.Framework.Color.White;
        deleteMenuLayer.Add(confirmText);

        deleteMenu = new Menu(device, controller);
        deleteMenu.X = deleteText.X;
        deleteMenu.Y = confirmText.Y + confirmText.Height + 10;
        deleteMenu.Lock();
        update.Add(deleteMenu);
        deleteMenuLayer.Add(deleteMenu);
        menuList.Add(deleteMenu);

        deleteOkButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
        deleteOkButton.Text = "Ok";
        deleteOkButton.Press += new EventHandler(onDeleteOk);
        deleteMenu.AddElement(deleteOkButton);

        deleteCancelButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
        deleteCancelButton.Text = "Cancel";
        deleteCancelButton.Press += new EventHandler(onDeleteCancel);
        deleteMenu.AddElement(deleteCancelButton, true);
        #endregion

        #region game menu

        gameMenu = new Menu(device, controller);
        gameMenu.X = highscoreBox.X + (highscoreBox.Width - buttonWidth) / 2;
        gameMenu.Y = highscoreBox.Y + highscoreBox.Height + boxMargin;
        gameMenu.Lock();
        update.Add(gameMenu);
        gameMenuLayer.Add(gameMenu);
        menuList.Add(gameMenu);

        playButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
        playButton.Text = "Play level";
        playButton.Press += new EventHandler(onPlay);
        gameMenu.AddElement(playButton);

        ghostButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
        ghostButton.Text = "Record ghost";
        ghostButton.Press += new EventHandler(onGhost);
        gameMenu.AddElement(ghostButton);

        viewButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
        viewButton.Text = "View ghost";
        viewButton.Press += new EventHandler(onView);
        gameMenu.AddElement(viewButton);

        playCancelButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
        playCancelButton.Text = "Cancel";
        playCancelButton.Press += new EventHandler(onPlayCancel);
        gameMenu.AddElement(playCancelButton, true);

        #endregion

        #region errmenu
        errUpdate = new ArrayList();

        errMenuLayer = new SpriteObject(device, null);

        //Background
        SpriteLine l = new SpriteLine(device);
        l.Width = 350;
        l.Thickness = 160;
        l.X = (width - l.Width) / 2;
        l.Y = height / 2;
        l.Tint = ColorX.FromArgb(30, 30, 30);
        errMenuLayer.Add(l);

        SpriteBox b = new SpriteBox(device);
        b.X = l.X;
        b.Y = l.Y - l.Thickness / 2;
        b.Width = l.Width;
        b.Height = l.Thickness;
        b.Tint = Microsoft.Xna.Framework.Color.CornflowerBlue;
        errMenuLayer.Add(b);

        //Header
        int margin = 10;
        SpriteObject errHeader = new SpriteObject(device, loadTexture(interfaceTex, "Hoki.textures.menu.text.error.dat"));
        errHeader.X = l.X + margin;
        errHeader.Y = l.Y - l.Thickness / 2 + margin;
        errMenuLayer.Add(errHeader);

        //Text
        errText = new SpriteText(device, menuFontSmall, (int)(l.Width - 2 * margin), (int)(l.Thickness - 4 * margin - buttonRight.Height - errHeader.Height));
        errText.X = l.X + margin;
        errText.Y = l.Y - l.Thickness / 2 + 2 * margin + errHeader.Height;
        errText.Tint = Microsoft.Xna.Framework.Color.White;
        errText.Format = FontDrawFlags.WordBreak | FontDrawFlags.Center | FontDrawFlags.VerticalCenter;
        errMenuLayer.Add(errText);

        //Menu
        errMenu = new Menu(device, controller);
        errMenu.X = l.X + (l.Width - buttonWidth) / 2;
        errMenu.Y = l.Y + l.Thickness / 2 - buttonRight.Height - margin;
        errMenu.Lock();
        errMenuLayer.Add(errMenu);
        errUpdate.Add(errMenu);

        //OK button
        errOkButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
        errOkButton.Text = "OK";
        errMenu.AddElement(errOkButton, true);

        #endregion

        #region level loader
        //HACKHACKHACK: load in levels directory to menu
        levelMenuLayer = new Fader(device, null);
        update.Add(levelMenuLayer);
        menuLayer.Add(levelMenuLayer);

        levelMenu = new Menu(device, controller);
        levelMenu.X = optionMenu.X;
        levelMenu.Y = optionMenu.Y;
        levelMenu.Lock();
        levelMenuLayer.Add(levelMenu);
        update.Add(levelMenu);
        menuList.Add(levelMenu);

        levelButtons = new DataButton[userLevels.Length];
        for (int i = 0; i < userLevels.Length; i++)
        {
            levelButtons[i] = new DataButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
            levelButtons[i].Data = userLevels[i];
            levelButtons[i].Text = userLevels[i].Name;
            levelButtons[i].Press += new EventHandler(onLevelButtonPress);
            levelMenu.AddElement(levelButtons[i]);
        }

        levelCancelButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
        levelCancelButton.Text = "Cancel";
        levelCancelButton.Press += new EventHandler(onLevelCancel);
        levelMenu.AddElement(levelCancelButton, true);
        #endregion

        #region ghost
        ghostMenu = new Menu(device, controller);
        ghostMenu.Lock();
        update.Add(ghostMenu);
        ghostMenuLayer.Add(ghostMenu);
        menuList.Add(ghostMenu);

        ghostText = new TextBox(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth, 10, this, ghostMenu);
        ghostText.Text = "<ghost name>";
        ghostMenu.AddElement(ghostText);

        ghostOkButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
        ghostOkButton.Text = "Save";
        ghostOkButton.Press += new EventHandler(onGhostOk);
        ghostMenu.AddElement(ghostOkButton);

        ghostCancelButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
        ghostCancelButton.Text = "Discard";
        ghostCancelButton.Press += new EventHandler(onGhostCancel);
        ghostMenu.AddElement(ghostCancelButton);

        //Center the menu on screen
        ghostMenu.X = (width - ghostMenu.Width) / 2;
        ghostMenu.Y = (height - ghostMenu.Height) / 2;
        #endregion

        #region view
        viewMenu = new Menu(device, controller);
        viewMenu.Lock();
        update.Add(viewMenu);
        viewMenuLayer.Add(viewMenu);
        menuList.Add(viewMenu);
        #endregion

        #endregion

        #region control help
        controlFader = new Fader(device, null);
        update.Add(controlFader);
        root.Add(controlFader);
        controlFader.FadeTarget = 255;

        margin = 4;
        int textWidth = 50, textHeight = 20;

        SpriteObject aIcon = new SpriteObject(device, aTex);
        aIcon.X = 10;
        aIcon.Y = height - aIcon.Height - 10;
        controlFader.Add(aIcon);

        SpriteText aText = new SpriteText(device, menuFontSmall, textWidth, textHeight);
        aText.Text = keyName(controller.GetKey(Hoki.Controls.A));
        aText.X = aIcon.X + aIcon.Width + margin;
        aText.Y = aIcon.Y;
        aText.Tint = Microsoft.Xna.Framework.Color.White;
        controlFader.Add(aText);

        SpriteObject bIcon = new SpriteObject(device, bTex);
        bIcon.X = aText.X + aText.Area().Width + 2 * margin;
        bIcon.Y = aText.Y;
        controlFader.Add(bIcon);

        SpriteText bText = new SpriteText(device, menuFontSmall, textWidth, textHeight);
        bText.Text = keyName(controller.GetKey(Hoki.Controls.B));
        bText.X = bIcon.X + bIcon.Width + margin;
        bText.Y = bIcon.Y;
        bText.Tint = Microsoft.Xna.Framework.Color.White;
        controlFader.Add(bText);

        SpriteObject lIcon = new SpriteObject(device, arrowTex);
        lIcon.X = bText.X + bText.Area().Width + 2 * margin;
        lIcon.Y = bText.Y;
        controlFader.Add(lIcon);

        SpriteText lText = new SpriteText(device, menuFontSmall, textWidth, textHeight);
        lText.Text = keyName(controller.GetKey(Hoki.Controls.Left));
        lText.X = lIcon.X + lIcon.Width + margin;
        lText.Y = lIcon.Y;
        lText.Tint = Microsoft.Xna.Framework.Color.White;
        controlFader.Add(lText);

        SpriteObject rIcon = new SpriteObject(device, null);
        rIcon.X = lText.X + lText.Area().Width + 2 * margin;
        rIcon.Y = lText.Y;
        controlFader.Add(rIcon);

        SpriteObject innerRIcon = new SpriteObject(device, arrowTex);   //lazy hack
        innerRIcon.Origin = innerRIcon.Position = new Vector2(innerRIcon.Width / 2, innerRIcon.Height / 2);
        innerRIcon.Rotation = FMath.PI;
        rIcon.Add(innerRIcon);

        SpriteText rText = new SpriteText(device, menuFontSmall, textWidth, textHeight);
        rText.Text = keyName(controller.GetKey(Hoki.Controls.Right));
        rText.X = rIcon.X + innerRIcon.Width + margin;
        rText.Y = rIcon.Y;
        rText.Tint = Microsoft.Xna.Framework.Color.White;
        controlFader.Add(rText);

        SpriteObject uIcon = new SpriteObject(device, null);
        uIcon.X = rText.X + rText.Area().Width + 2 * margin;
        uIcon.Y = rText.Y;
        controlFader.Add(uIcon);

        SpriteObject innerUIcon = new SpriteObject(device, arrowTex);   //lazy hack
        innerUIcon.Origin = innerUIcon.Position = new Vector2(innerUIcon.Width / 2, innerUIcon.Height / 2);
        innerUIcon.Rotation = FMath.PI / 2;
        uIcon.Add(innerUIcon);

        SpriteText uText = new SpriteText(device, menuFontSmall, textWidth, textHeight);
        uText.Text = keyName(controller.GetKey(Hoki.Controls.Up));
        uText.X = uIcon.X + innerUIcon.Width + margin;
        uText.Y = uIcon.Y;
        uText.Tint = Microsoft.Xna.Framework.Color.White;
        controlFader.Add(uText);

        SpriteObject dIcon = new SpriteObject(device, null);
        dIcon.X = uText.X + uText.Area().Width + 2 * margin;
        dIcon.Y = uText.Y;
        controlFader.Add(dIcon);

        SpriteObject innerDIcon = new SpriteObject(device, arrowTex);   //lazy hack
        innerDIcon.Origin = innerDIcon.Position = new Vector2(innerDIcon.Width / 2, innerDIcon.Height / 2);
        innerDIcon.Rotation = -FMath.PI / 2;
        dIcon.Add(innerDIcon);

        SpriteText dText = new SpriteText(device, menuFontSmall, textWidth, textHeight);
        dText.Text = keyName(controller.GetKey(Hoki.Controls.Down));
        dText.X = dIcon.X + innerDIcon.Width + margin;
        dText.Y = dIcon.Y;
        dText.Tint = Microsoft.Xna.Framework.Color.White;
        controlFader.Add(dText);

        SpriteObject pauseIcon = new SpriteObject(device, pauseTex);
        pauseIcon.X = dText.X + dText.Area().Width + 2 * margin;
        pauseIcon.Y = dText.Y;
        controlFader.Add(pauseIcon);

        SpriteText pauseText = new SpriteText(device, menuFontSmall, textWidth, textHeight);
        pauseText.Text = keyName(controller.GetKey(Hoki.Controls.Start));
        pauseText.X = pauseIcon.X + pauseIcon.Width + margin;
        pauseText.Y = pauseIcon.Y;
        pauseText.Tint = Microsoft.Xna.Framework.Color.White;
        controlFader.Add(pauseText);
        #endregion

        #region names
        //Add the planet names and numbers to the layer
        foreach (Fader name in planetNames)
        {
            textLayer.Add(name);
            update.Add(name);
            name.Y = planetNameHeight;
        }
        foreach (Fader number in planetNumbers)
        {
            textLayer.Add(number);
            update.Add(number);
            number.Y = planetNameHeight;
        }
        #endregion

        #region levels

        //Make an array of level positions and put markers at those positions
        levelPos = new Vector2[] {
            new Vector2(0,0),
            new Vector2(125,0),
            new Vector2(250,0),
            new Vector2(375,0),
            new Vector2(504,-14),
            new Vector2(600,48),
            new Vector2(710,30),
            new Vector2(818,36),
            new Vector2(910,74),
            new Vector2(982,146),
            new Vector2(1078,172),
            new Vector2(1162,126),
            new Vector2(1206,46),
            new Vector2(1246,-36),
            new Vector2(1326,-76),
            new Vector2(1424,-62),
            new Vector2(1520,-32),
            new Vector2(1610,-88),
            new Vector2(1664,-186),
            new Vector2(1708,-288),
            new Vector2(1772,-382),
            new Vector2(1862,-432),
            new Vector2(1954,-462),
            new Vector2(2064,-468),
            new Vector2(2164,-434),
            new Vector2(2249,-392),
            new Vector2(2410,-392)
        };

        //HACK: Scale the points
        for (int i = 0; i < levelPos.Length; i++) levelPos[i] = Vector2.Multiply(levelPos[i], 1.5f);

        Vector2 markerPos = mainMenuPos + centerPoint + new Vector2(width, 0);  //Make a base position for all markers

        markers = new SpriteObject[levelPos.Length];
        for (int i = 0; i < levelPos.Length; i++)
        {
            markers[i] = new SpriteObject(device, markerTex);

            Vector2 pos = levelPos[i] + markerPos;
            markers[i].Position = pos;
            markers[i].Origin = new Vector2(markers[i].Width / 2, markers[i].Height / 2);
            markerLayer.Add(markers[i]);
        }

        //			mars.Position=new Vector2(600,50)+markerPos;
        //			markerLayer.Add(mars);

        SpriteTexture sphereTex = loadTexture(interfaceTex, "Hoki.textures.menu.sphere.dat");

        Planet tilePlanet = new Planet(device, loadSizeTexture("Hoki.textures.game.themes.tiles.bg.png").Tex, sphereTex, this, 40, 60, 1, new Vector2(0.3f, 0.03f));
        tilePlanet.Position = new Vector2(1100, -100) + markerPos;
        markerLayer.Add(tilePlanet);
        update.Add(tilePlanet);

        Planet castlePlanet = new Planet(device, loadSizeTexture("Hoki.textures.game.themes.castle.bg.png").Tex, sphereTex, this, 45, 80, 0.4f, new Vector2(0.05f, 0.02f));
        castlePlanet.Position = new Vector2(1430, 340) + markerPos;
        markerLayer.Add(castlePlanet);
        update.Add(castlePlanet);

        Planet lavaPlanet = new Planet(device, loadSizeTexture("Hoki.textures.game.themes.lava.fg.png").Tex, sphereTex, this, 50, 100, 0.4f, new Vector2(0.1f, 0.05f));
        lavaPlanet.Position = new Vector2(1950, 200) + markerPos;
        markerLayer.Add(lavaPlanet);
        update.Add(lavaPlanet);

        Planet woodPlanet = new Planet(device, loadSizeTexture("Hoki.textures.game.themes.wood.bg.png").Tex, sphereTex, this, 40, 70, 0.6f, new Vector2(0.4f, 0.02f));
        woodPlanet.Position = new Vector2(2170, -220) + markerPos;
        markerLayer.Add(woodPlanet);
        update.Add(woodPlanet);

        Planet sumiePlanet = new Planet(device, loadSizeTexture("Hoki.textures.game.themes.sumie.fg.png").Tex, sphereTex, this, 40, 50, 2f, new Vector2(0.3f, 0.13f));
        sumiePlanet.Position = new Vector2(2700, -300) + markerPos;
        markerLayer.Add(sumiePlanet);
        update.Add(sumiePlanet);

        Planet bambooPlanet = new Planet(device, loadSizeTexture("Hoki.textures.game.themes.bamboo.bg.png").Tex, sphereTex, this, 40, 65, 1f, new Vector2(0.04f, 0.08f));
        bambooPlanet.Position = new Vector2(2780, -800) + markerPos;
        markerLayer.Add(bambooPlanet);
        update.Add(bambooPlanet);

        Planet medievalPlanet = new Planet(device, loadSizeTexture("Hoki.textures.game.themes.medieval.bg.png").Tex, sphereTex, this, 40, 86, 1f, new Vector2(0.3f, 0.08f));
        medievalPlanet.Position = new Vector2(3240, -530) + markerPos;
        markerLayer.Add(medievalPlanet);
        update.Add(medievalPlanet);

        //			neptune.Position=new Vector2(1500,330)+markerPos;
        //			markerLayer.Add(neptune);

        #endregion

        #region state

        //Determine the appropriate state
        if (playingMainGame)
        {
            playingMainGame = false;    //Don't assume the player will play another level in the main game

            Score levelScore = player.GetScore(gameLevels[oldLevel].Hash);
            bool justCompleted = oldLevel == gameLevels.Length - 2 && (levelScore != null && (!levelScore.Easy || player.Easy)) && (!player.CompletedEasy || (!player.Easy && !player.CompletedNormal));

            player.RefreshStatus(gameLevels, times);

            if (justCompleted)
            {
                //The player just completed the game, take him to a special screen
                SpriteObject congrats = new SpriteObject(device, loadTexture(interfaceTex, "Hoki.textures.menu.congrats.dat"));
                congrats.Position = new Vector2(2000, -1000);
                menuLayer.Add(congrats);

                SpriteText endText = new SpriteText(device, menuFontSmall, 400, 100);
                endText.Tint = Microsoft.Xna.Framework.Color.White;
                endText.Format = FontDrawFlags.Center;
                endText.X = congrats.X - (endText.Width - congrats.Width) / 2;
                endText.Y = congrats.Y + congrats.Height + 10.5f;
                endText.Text = "Completed in " + Score.GetTimeString(player.TotalTime);
                menuLayer.Add(endText);

                endMenu = new Menu(device, controller);
                endMenu.X = congrats.X - (buttonWidth - congrats.Width) / 2;
                endMenu.Y = endText.Y + 20;
                menuLayer.Add(endMenu);

                endKey = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
                endKey.Text = "Ok";
                endKey.Select();
                endKey.Press += new EventHandler(onEnd);
                endMenu.AddElement(endKey, true);

                stars.SetTo(congrats.Position + new Vector2(congrats.Width / 2, congrats.Height / 2) - centerPoint);
                levelSelecting = false;

                startMenuLayer.FadeTarget = 0;
                startMenu.Lock();

                mainMenuLayer.FadeTarget = 0;
                mainMenu.Lock();
            }
            else
            {
                stars.SetTo(markers[oldLevel].Position - centerPoint);
                stars.MoveTo(markers[currentLevel].Position - centerPoint);

                nameFade(currentLevel, -1);

                setupMarkers();

                updateInterface(currentLevel);

                //Fade the start menu
                startMenuLayer.FadeTarget = 0;
                startMenu.Lock();

                //Pop up the ghost menu
                if (recorder != null)
                {
                    levelSelecting = false;
                    ghostMenuLayer.FadeTarget = TransformedObject.MaxAlpha;
                    ghostMenu.Unlock();
                }
                else
                    levelSelecting = true;
            }
        }
        else
        {
            levelSelecting = false;
            if (loadingLevel >= 0)
            {
                startMenuLayer.FadeTarget = 100;
                startMenu.Lock();

                mainMenu.Select(1);
                mainMenu.Lock();
                mainMenuLayer.FadeTarget = mainMenuLayer.Alpha = 100;

                levelMenuLayer.FadeTarget = levelMenuLayer.Alpha = TransformedObject.MaxAlpha;
                levelMenu.Unlock();
                levelMenu.Select(loadingLevel);
                stars.SetTo(mainMenuPos);

                loadingLevel = -1;
            }
        }

        #endregion

        #region sound
        currentSong = menuSong = new SimpleSong(MusicDir + "cf.ogg");
        if (musicOn) currentSong.Play();

        boneSong = new SimpleSong(MusicDir + "bs.ogg");
        #endregion

        bonesaw = new SpriteObject(device, loadTexture(interfaceTex, "Hoki.textures.menu.bonesaw.dat"));
        bonesaw.X = (startMenu.Width - bonesaw.Width) / 2;
        bonesaw.Y = -150;
        bonesaw.Visible = false;
        startMenu.Add(bonesaw);

        boneText = new SpriteText(device, boneFont, width, 100);
        boneText.X = (startMenu.Width - boneText.Width) / 2;
        boneText.Y = 60;
        boneText.Format = FontDrawFlags.Center;
        boneText.Text = "BONESAW";
        boneText.Tint = Microsoft.Xna.Framework.Color.White;
        boneText.Visible = false;
        startMenu.Add(boneText);

        boneTimer = boneWait;
        boneStarted = false;
    }

    private void menuEnd()
    {
        stars = null;
        root.Clear();               //Clear graphics
        update.Clear();             //Clear all updateables

        //Unhook events
        startButton.Press -= new EventHandler(onGameStart);
        newGameButton.Press -= new EventHandler(onNewGame);
        loadLevelButton.Press -= new EventHandler(onLoadLevel);
        optionsButton.Press -= new EventHandler(onOptions);
        creditsButton.Press -= new EventHandler(onCredits);
        exitButton.Press -= new EventHandler(onExit);
        optionsOkButton.Press -= new EventHandler(onOptionsOk);
        creditsOkButton.Press -= new EventHandler(onCreditsOk);
        levelCancelButton.Press -= new EventHandler(onLevelCancel);
        playerCancelButton.Press -= new EventHandler(onPlayerCancel);
        newPlayerButton.Press -= new EventHandler(onNewPlayer);
        newPlayerOkButton.Press -= new EventHandler(onNewPlayerOk);
        newPlayerCancelButton.Press -= new EventHandler(onNewPlayerCancel);
        signoutButton.Press -= new EventHandler(onSignOut);
        deleteButton.Press -= new EventHandler(onDelete);
        deleteOkButton.Press -= new EventHandler(onDeleteOk);
        deleteCancelButton.Press -= new EventHandler(onDeleteCancel);
        playButton.Press -= new EventHandler(onPlay);
        ghostButton.Press -= new EventHandler(onGhost);
        viewButton.Press -= new EventHandler(onView);
        playCancelButton.Press -= new EventHandler(onPlayCancel);
        ghostOkButton.Press -= new EventHandler(onGhostOk);
        ghostCancelButton.Press -= new EventHandler(onGhostCancel);
        volumeButton.Press -= new EventHandler(this.onVolume);
        volumeSlider.Change -= new EventHandler(onVolumeChange);
        volumeSlider.Press -= new EventHandler(onVolumeOk);
        musicButton.Press -= new EventHandler(onMusicToggle);
        fxButton.Press -= new EventHandler(onFXToggle);
        difficultyButton.Press -= new EventHandler(onDifficultyToggle);
        if (endKey != null)
        {
            endKey.Press -= new EventHandler(onEnd);
            endKey = null;
        }

        for (int i = 0; i < levelButtons.Length; i++) levelButtons[i].Press -= new EventHandler(onLevelButtonPress);
        for (int i = 0; i < playerButtons.Count; i++)
        {
            ((DataButton)playerButtons[i]).Press -= new EventHandler(onPlayerSelect);
            ((DataButton)playerButtons[i]).Over -= new EventHandler(onPlayerOver);
            ((DataButton)playerButtons[i]).Out -= new EventHandler(onPlayerOut);
        }
        playerButtons = null;
        playerButton = null;

        update.Clear();
        root.Clear();

        //Empty the variables
        interfaceTex = null;
        easyTex = null;
        trophyTex = null;
        perfectTex = null;
        boxTex = UIBoxTex.Empty;
        planetNames = null;
        planetNumbers = null;
        starTex = null;
        iconLayer = null;
        textLayer = null;
        lineLayer = null;
        interfaceLayer = null;
        markers = null;
        levelPos = null;

        menuLayer = null;
        playerMenuLayer = null;
        newPlayerMenuLayer = null;
        mainMenuLayer = null;
        optionMenuLayer = null;
        creditsMenuLayer = null;
        levelMenuLayer = null;
        startMenuLayer = null;
        deleteMenuLayer = null;
        gameMenuLayer = null;
        ghostMenuLayer = null;
        viewMenuLayer = null;
        volumeMenuLayer = null;
        boxLayer = null;
        errMenuLayer = null;
        errText = null;
        controlFader = null;

        previewBox = null;
        playerTopBox = null;
        highscoreBox = null;
        bestTimeText = null;
        highScoreText = null;
        boneText = null;

        playerDetails = null;
        playerStar = null;
        playerBronze = null;
        playerSilver = null;
        playerGold = null;

        mainMenu.Unhook();
        optionMenu.Unhook();
        creditsMenu.Unhook();
        levelMenu.Unhook();
        startMenu.Unhook();
        playerMenu.Unhook();
        newPlayerMenu.Unhook();
        deleteMenu.Unhook();
        gameMenu.Unhook();
        ghostMenu.Unhook();
        volumeMenu.Unhook();
        viewMenu.Unhook();
        errMenu.Unhook();
        if (endMenu != null) endMenu.Unhook();

        mainMenu = null;
        optionMenu = null;
        creditsMenu = null;
        levelMenu = null;
        startMenu = null;
        playerMenu = null;
        newPlayerMenu = null;
        deleteMenu = null;
        gameMenu = null;
        ghostMenu = null;
        volumeMenu = null;
        errMenu = null;
        activeMenu = null;
        endMenu = null;

        previewTex = null;
        mapPreview = null;      //Menu sublayers

        clearViewMenu();
        viewMenu.Unhook();
        viewMenu = null;

        startButton = null;
        newGameButton = null;
        loadLevelButton = null;
        optionsButton = null;
        creditsButton = null;
        exitButton = null;
        optionsOkButton = null;
        creditsOkButton = null;
        levelCancelButton = null;
        playerCancelButton = null;
        newPlayerButton = null;
        newPlayerOkButton = null;
        newPlayerCancelButton = null;
        signoutButton = null;
        deleteButton = null;
        deleteOkButton = null;
        deleteCancelButton = null;
        playButton = null;
        ghostButton = null;
        viewButton = null;
        playCancelButton = null;
        ghostOkButton = null;
        ghostCancelButton = null;
        viewCancelButton = null;
        volumeButton = null;
        volumeSlider = null;
        levelButtons = null;
        ghostButtons = null;
        errOkButton = null;
        musicButton = null;
        fxButton = null;
        difficultyButton = null;

        bonesaw = null;
        boneText = null;

        errUpdate = null;
        menuList = null;

        ghostText.Unhook();
        newPlayerInput.Unhook();
        ghostText = null;
        newPlayerInput = null;

        planetNames = planetNumbers = null;

        currentSong.Stop();
        currentSong = null;

        //TEST: GARBAGE COLLECT
        GC.Collect();
    }

    private void error(string message)
    {
        paused = true;
        errText.Text = message;
        root.Add(errMenuLayer);
        errMenu.Unlock();
        errOkButton.Press += new EventHandler(onErrOk);

        foreach (Menu m in menuList)
        {
            if (!m.Locked)
            {
                activeMenu = m;
                m.Lock();
                break;
            }
        }
    }

    #endregion

    #region main
    public void mainBegin()
    {
        //Set filters
        Renderer.Filter = TextureFilter.Linear;

        //device.SamplerState[0].MinFilter=minFilter;
        //device.SamplerState[0].MagFilter=magFilter;

        //Reset the timers
        started = false;
        finished = false;
        dead = false;
        gameTime = 0;

        //Create a map if needed
        /* This code is a little dangerous. generateMap() only tries to make a map,
			 * doesn't guarantee anything. Theoretically it only gets called if main is
			 * restarting, in which case this map has been loaded before and works.
			 * However, if another entry point to the main gamestate is created that
			 * doesn't require validation of the map in advance, it could cause an
			 * issue. */
        if (map == null) generateMap();

        //Add the map to root and update
        root.Add(map);
        update.Add(map);

        //Create a layer to put interfaces on and add it to root
        dataLayer = new SpriteObject(device, null);
        dataLayer.DrawFilter = TextureFilter.Point;
        root.Add(dataLayer);

        //User interface vars
        string path = "Hoki.textures.gameui.";
        gameUITex = loadSizeTexture(path + "gameui.png");

        //Make a health indicator
        SpriteTexture dotTex = loadTexture(gameUITex, path + "dot.dat");
        healthIndicator = new SpriteObject[Heli.FullHealth];
        for (int i = 0; i < healthIndicator.Length; i++)
        {
            healthIndicator[i] = new SpriteObject(device, dotTex);
            healthIndicator[i].X = 10 + i * (healthIndicator[i].Width + 2);
            healthIndicator[i].Y = 10;
            dataLayer.Add(healthIndicator[i]);
        }

        //Make a timer
        timeGraphic = new SpriteTime(device, loadTexture(gameUITex, path + "bignumbers.dat"), loadTexture(gameUITex, path + "smallnumbers.dat"));
        timeGraphic.X = 20 + healthIndicator.Length * (healthIndicator[0].Width + 2);
        timeGraphic.Y = 10;
        timeGraphic.setTime(Score.GetTimeString(0));
        dataLayer.Add(timeGraphic);

        //Make the heli TODO: the texture loading should happen earlier
        heliSizeTex = loadSizeTexture("Hoki.textures.game.sprites.png");
        heliTex = loadTexture(heliSizeTex, "Hoki.textures.game.heli.dat");
        easyHeliTex = loadTexture(heliSizeTex, "Hoki.textures.game.easyheli.dat");
        heli = new Heli(device, player.Easy ? easyHeliTex : heliTex, map);
        heli.AntiAlias = antialias;

        //Capture the heli's events
        heli.Hit += new HitEventHandler(onHit);
        heli.Start += new EventHandler(onStart);
        heli.Finish += new EventHandler(onFinish);
        heli.Die += new EventHandler(onDie);
        finished = false;   //The game is not over yet

        //Set location
        Vector2 startPoint = new Vector2(map.StartPad.X + Map.PadSize / 2, map.StartPad.Y + Map.PadSize / 2);
        heli.Midpoint = startPoint;
        heli.Controller = controller;

        //Add to the update list and the map
        update.Add(heli);
        map.Add(heli);

        //Set the regular heli as focused by default
        focusedHeli = heli;

        //Make the ghost heli and controler, if necessary
        StreamReader ghostReader = null;
        if (viewGhost != null) ghostReader = new StreamReader(Path.Combine(AppContext.BaseDirectory, "ghosts") + Path.DirectorySeparatorChar + viewGhost + ".ghost");
        else if (map.Ghost != null) ghostReader = new StreamReader(getStream("Hoki.data.ghosts." + map.Ghost));

        if (ghostReader != null)
        {
            //Make a ghost controller
            ghostController = new GhostController();
            ghostReader.ReadLine();                             //Throw away the first line
            ghostController.Construct(ghostReader.ReadToEnd()); //TODO: file picking
            ghostReader.Close();
            update.Add(ghostController);

            //Make a ghost heli
            ghost = new Heli(device, heliTex, map);
            ghost.Alpha = 150;
            ghost.Midpoint = startPoint;
            ghost.Controller = ghostController;
            update.Add(ghost);
            map.Add(ghost);
        }

        //Make the ghost recorder, if necessary
        if (recording)
        {
            recorder = new GhostRecorder(controller);
            update.Add(recorder);
        }

        //Play the music
        if (musicOn && map.Song != null)
        {
            currentSong = map.Song;
            currentSong.Play();
        }

        //Make the start menu
        pauseUpdate = new ArrayList();

        pauseMenuLayer = new Fader(device, null);
        pauseMenuLayer.RateScale = 2;
        pauseUpdate.Add(pauseMenuLayer);
        root.Add(pauseMenuLayer);

        pauseMenu = new Menu(device, controller);
        pauseMenu.DrawFilter = TextureFilter.Point;
        pauseMenu.Lock();
        pauseUpdate.Add(pauseMenu);
        pauseMenuLayer.Add(pauseMenu);

        continueButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
        continueButton.Text = "Continue";
        continueButton.Press += new EventHandler(onContinue);
        pauseMenu.AddElement(continueButton);

        restartButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
        restartButton.Text = "Restart";
        restartButton.Press += new EventHandler(onRestart);
        pauseMenu.AddElement(restartButton);

        quitButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
        quitButton.Text = "Quit to menu";
        quitButton.Press += new EventHandler(onQuitToMenu);
        pauseMenu.AddElement(quitButton);

        //Position the menu
        pauseMenu.X = (width - buttonWidth) / 2;
        pauseMenu.Y = (height - pauseMenu.Height) / 2;
    }

    /// <summary>
    /// Appends the recorded heli path (one attempt) to the --trace file. Best-effort.
    /// </summary>
    private void flushTrace()
    {
        if (tracePath == null || tracePoints.Count == 0) return;
        try
        {
            using (StreamWriter w = new StreamWriter(tracePath, true))
            {
                //Rotation as integer millirads: locale-proof, plenty of precision
                foreach (Vector3 p in tracePoints) w.WriteLine((int)p.X + "," + (int)p.Y + "," + (int)MathF.Round(p.Z * 1000));
                w.WriteLine("---");
            }
        }
        catch { }
        tracePoints.Clear();
    }

    protected override void OnExiting(object sender, ExitingEventArgs e)
    {
        flushTrace();   //Window closed mid-attempt
        base.OnExiting(sender, e);
    }

    public void mainEnd()
    {
        flushTrace();

        //Remove graphics
        root.Clear();
        update.Clear();

        pauseMenu.Unhook();

        continueButton.Press -= new EventHandler(onContinue);
        quitButton.Press -= new EventHandler(onQuitToMenu);
        restartButton.Press -= new EventHandler(onRestart);

        //Clear the updates
        update.Clear();

        //Release events
        heli.Hit -= new HitEventHandler(onHit);
        heli.Start -= new EventHandler(onStart);
        heli.Finish -= new EventHandler(onFinish);
        heli.Die -= new EventHandler(onDie);

        //Empty variables
        dataLayer = null;
        gameUITex = null;
        heliSizeTex = null;
        healthIndicator = null;
        focusedHeli = null;
        heliTex = null;
        easyHeliTex = null;

        heli = null;
        ghost = null;
        ghostController = null;
        map = null;
        finished = false;
        ghosting = recording = false;
        timeGraphic = null;
        pauseUpdate = null;

        continueButton = null;
        restartButton = null;
        quitButton = null;
        pauseMenuLayer = null;
        pauseMenu = null;

        //Stop the music
        if (currentSong != null)
        {
            currentSong.Stop();
            currentSong = null;
        }

        //TEST: GARBAGE COLLECT
        GC.Collect();
    }
    #endregion

    #endregion

    #region menu events

    private void onVolumeChange(object sender, EventArgs e)
    {
        Song.Volume = volumeSlider.Value * 100;
        FXVolume = volumeSlider.Value;  //Effects are played at this volume (0-1)
    }

    private void onVolume(object sender, EventArgs e)
    {
        volumeMenu.Unlock();
        volumeMenuLayer.FadeTarget = TransformedObject.MaxAlpha;

        optionMenu.Lock();
        optionMenuLayer.FadeTarget = 100;
    }

    private void onVolumeOk(object sender, EventArgs e)
    {
        //Save the volume to file
        saveConfig();

        volumeMenu.Lock();
        volumeMenuLayer.FadeTarget = 0;

        optionMenu.Unlock();
        optionMenuLayer.FadeTarget = TransformedObject.MaxAlpha;
    }

    private void onPlay(object sender, EventArgs e)
    {
        levelSelecting = true;
        playingMainGame = true;
        play(gameLevels[currentLevel]);
    }

    private void onPlayCancel(object sender, EventArgs e)
    {
        levelSelecting = true;

        gameMenuLayer.FadeTarget = 0;
        gameMenu.Lock();
        hideBoxes();
    }

    private void onGhost(object sender, EventArgs e)
    {
        recording = true;
        levelSelecting = true;
        playingMainGame = true;
        play(gameLevels[currentLevel]);
    }

    private void onView(object sender, EventArgs e)
    {
        //Clear the menu's elements if it was used before
        clearViewMenu();

        //Add the ghosts to the menu
        Level level = gameLevels[currentLevel];
        ArrayList ghosts = level.Ghosts;

        ghostButtons = new DataButton[ghosts.Count];
        for (int i = 0; i < ghostButtons.Length; i++)
        {
            ghostButtons[i] = new DataButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
            ghostButtons[i].Text = ((String)ghosts[i]).Substring(0, Math.Min(8, ((String)ghosts[i]).Length));   //Limit to 8 characters in name
            ghostButtons[i].Data = ghosts[i];
            ghostButtons[i].Press += new EventHandler(onGhostView);
            viewMenu.AddElement(ghostButtons[i]);
        }

        //Add the cancel button
        viewCancelButton = new KeyButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, buttonWidth);
        viewCancelButton.Text = "Cancel";
        viewCancelButton.Press += new EventHandler(onViewCancel);
        viewMenu.AddElement(viewCancelButton, true);

        //Fade it in and unlock it
        viewMenuLayer.FadeTarget = TransformedObject.MaxAlpha;
        viewMenu.Unlock();

        //Center the viewmenu on screen
        viewMenu.X = (width - viewMenu.Width) / 2;
        viewMenu.Y = (height - viewMenu.Height) / 2;

        //Fade the game menu out
        gameMenuLayer.FadeTarget = 100;
        gameMenu.Lock();
    }

    private void clearViewMenu()
    {
        if (ghostButtons != null)
        {
            foreach (DataButton button in ghostButtons) button.Press -= new EventHandler(onGhostView);  //Unhook events
            viewCancelButton.Press -= new EventHandler(onViewCancel);
            viewCancelButton = null;
            viewMenu.ClearElements();
        }
    }

    private void onLevelButtonPress(object sender, EventArgs e)
    {
        loadingLevel = levelMenu.Index;
        play((Level)((DataButton)sender).Data);
    }

    private void play(Level l)
    {
        playLevel = l;

        //Try to generate a map
        if (!generateMap())
        {
            if (gameState == GameState.None)
            {
                //Playtest launch with a bad map: no menu exists to show the error in
                Console.Error.WriteLine("playtest: the map cannot be read; it may be invalid or corrupted");
                Exit();
                return;
            }
            error("The map you selected cannot be read. It may be invalid or corrupted.");
            return;
        }
        setGameState(GameState.Main);
    }

    private bool generateMap()
    {
        //Try to make a map
        try
        {
            map = Map.FromString(playLevel.GameMap, device, this);
            map.Antialias = antialias;
        }
        catch (Exception e)
        {
            System.Diagnostics.Trace.WriteLine("Map generation error: " + e.Message);
        }
        //Failure
        if (map == null || !map.HasStartPad)
        {
            map = null;
            return false;
        }
        //Success
        return true;
    }

    private void onGameStart(object sender, EventArgs e)
    {
        //Go to the player select menu
        startMenuLayer.FadeTarget = 0;
        startMenu.Lock();

        playerMenuLayer.FadeTarget = TransformedObject.MaxAlpha;
        playerMenu.Unlock();

        stars.MoveTo(playerMenuPos);
    }

    private void onPlayerSelect(object sender, EventArgs e)
    {
        //Set the current player
        player = (Player)((DataButton)sender).Data;
        playerButton = (DataButton)sender;

        //Go to the main menu
        playerMenuLayer.FadeTarget = 0;
        playerMenu.Lock();

        mainMenuLayer.FadeTarget = TransformedObject.MaxAlpha;
        mainMenu.Unlock();

        stars.MoveTo(mainMenuPos);

        //Update the difficulty button text based on preference
        difficultyButton.Text = "Difficulty: " + (player.Easy ? "easy" : "normal");

        //Color and connect the markers
        setupMarkers();

        //Start at the last unlocked level
        currentLevel = lastUnlockedLevel;
    }

    private void onPlayerOver(object sender, EventArgs e)
    {
        DataButton button = (DataButton)sender;
        Player player = (Player)button.Data;

        Vector2 position = new Vector2(button.X + 200, button.Y);

        playerDetails.Position = position;
        playerDetails.Text = "";
        playerDetails.Visible = true;

        position.Y += 24;

        if (player.CompletedEasy)
        {
            playerDetails.Text += "Completed " + Score.GetTimeString(player.TotalTime);
            if (!player.CompletedNormal) playerDetails.Text += " (easy)";
        }
        else
        {
            playerDetails.Text += levelName(player.Highest + 1);
            if (player.HighestEasy && player.Highest > 0) playerDetails.Text += " (easy)";
        }
        playerDetails.Text += "\n";

        if (player.Perfects > 0)
        {
            playerDetails.Text += "     " + player.Perfects + "\n";
            playerStar.Visible = true;
            playerStar.Position = position;
            position.Y += 21;
        }
        else playerStar.Visible = false;

        if (player.Trophies[0] > 0)
        {
            playerDetails.Text += "     " + player.Trophies[0] + "\n";
            playerGold.Visible = true;
            playerGold.Position = position;
            position.Y += 21;
        }
        else playerGold.Visible = false;

        if (player.Trophies[1] > 0)
        {
            playerDetails.Text += "     " + player.Trophies[1] + "\n";
            playerSilver.Visible = true;
            playerSilver.Position = position;
            position.Y += 21;
        }
        else playerSilver.Visible = false;

        if (player.Trophies[2] > 0)
        {
            playerDetails.Text += "     " + player.Trophies[2] + "\n";
            playerBronze.Visible = true;
            playerBronze.Position = position;
        }
        else playerBronze.Visible = false;
    }

    private void onPlayerOut(object sender, EventArgs e)
    {
        playerDetails.Visible = false;
        playerStar.Visible = false;
        playerBronze.Visible = false;
        playerSilver.Visible = false;
        playerGold.Visible = false;
    }

    private string levelName(int level)
    {
        string[] names ={
            "Desert",
            "Castle",
            "Lava",
            "Parquet",
            "Sumi-e",
            "Bamboo",
            "Cannon"
        };

        if (level < 5) return "Training " + (level + 1);
        else
        {
            int i = level - 5;
            return names[i / 3] + " " + ((i % 3) + 1);
        }
    }

    private void setupMarkers()
    {
        //Clear the lines and icons
        lineLayer.Clear();
        iconLayer.Clear();

        //All unlocked by default
        lastUnlockedLevel = markers.Length - 1;
        checkSecretLock();

        if (player.Perfects < markers.Length - 1) markers[markers.Length - 1].Visible = false;
        for (int i = 0; i < markers.Length; i++)
        {
            if (i == markers.Length - 1 && player.Perfects < markers.Length - 1)
            {
                markers[i].Visible = false;
                break;
            }

            connectMarker(i);

            Score score = player.GetScore(gameLevels[i].Hash);

            if (score == null || (score.Easy && !player.Easy))
            {
                //Last marker, clear the rest and break
                lastUnlockedLevel = i;
                for (int n = i; n < markers.Length; n++) markers[n].Frame = 0;
                if (currentLevel > i) currentLevel = i;
                break;
            }
            else
            {
                markers[i].Frame = 1;

                //Add icons above the score where applicable (not allowed for easy scores)
                if (score.Easy)
                {
                    SpriteObject easyMark = new SpriteObject(device, easyTex);
                    easyMark.Origin = new Vector2(easyMark.Width / 2, easyMark.Height / 2);
                    easyMark.X = markers[i].X;
                    easyMark.Y = markers[i].Y - markers[i].Height - 2;
                    iconLayer.Add(easyMark);
                }
                else
                {
                    SpriteObject trophy = null;
                    if (i < times.GetLength(0) && score.Time <= times[i, 2])
                    {
                        trophy = new SpriteObject(device, trophyTex);
                        trophy.Origin = new Vector2(trophy.Width / 2, trophy.Height / 2);
                        trophy.X = markers[i].X;
                        trophy.Y = markers[i].Y - markers[i].Height - 2;
                        if (score.Time < times[i, 0]) trophy.Frame = 2;
                        else if (score.Time < times[i, 1]) trophy.Frame = 1;
                        iconLayer.Add(trophy);
                    }

                    if (score.Perfect)
                    {
                        SpriteObject star = new SpriteObject(device, perfectTex);
                        star.Origin = new Vector2(star.Width / 2, star.Height / 2);
                        star.X = markers[i].X;
                        star.Y = markers[i].Y - markers[i].Height - 2;
                        if (trophy != null)
                        {
                            trophy.X -= trophy.Width / 2 + 2;
                            star.X += star.Width / 2 + 2;
                        }
                        iconLayer.Add(star);
                    }
                }
            }
        }
    }

    private void connectMarker(int index)
    {
        //Connect to the marker
        if (index != 0)
        {
            SpriteLine line = new SpriteLine(device);
            line.Position = markers[index - 1].Position;

            Vector2 lineSize = markers[index].Position - line.Position;
            line.Width = lineSize.X;
            line.Height = lineSize.Y;
            line.Thickness = 2;
            line.Antialias = true;
            line.Tint = Microsoft.Xna.Framework.Color.White;

            lineLayer.Add(line);
        }
    }

    private void onNewPlayer(object sender, EventArgs e)
    {
        //Go to the new player menu
        playerMenuLayer.FadeTarget = 100;
        playerMenu.Lock();

        newPlayerMenuLayer.FadeTarget = TransformedObject.MaxAlpha;
        newPlayerMenu.Unlock();

        //Reset the input
        newPlayerInput.Text = "<Type name here>";
        newPlayerInput.DefaultCleared = false;
        newPlayerMenu.Select(0);
    }

    private void onNewPlayerOk(object sender, EventArgs e)
    {
        //Ensure a name has been entered
        if (!newPlayerInput.DefaultCleared || newPlayerInput.Text.Length == 0) return; //TODO: Error message+sound

        //Make sure the name is not already in use
        foreach (Player p in players) if (p.Name.Equals(newPlayerInput.Text)) return; //TODO: Error message+sound

        //Create the player
        Player newPlayer = new Player(newPlayerInput.Text);
        players.Add(newPlayer);
        newPlayer.RefreshStatus(gameLevels, times);

        //Write the player to file
        writePlayers();

        //Create a new button for it
        DataButton playerButton = new DataButton(device, buttonLeft, buttonMiddle, buttonRight, menuFontSmall, pButtonWidth);
        playerButtons.Add(playerButton);
        playerButton.Text = newPlayer.Name;
        playerButton.Data = newPlayer;
        playerButton.Press += new EventHandler(onPlayerSelect);
        playerButton.Over += new EventHandler(onPlayerOver);
        playerButton.Out += new EventHandler(onPlayerOut);

        //Insert it into the menu
        playerMenu.AddElement(playerButton, false, playerMenu.Elements - 2);

        //Fade the new player menu
        playerMenu.Unlock();
        playerMenuLayer.FadeTarget = TransformedObject.MaxAlpha;

        newPlayerMenu.Lock();
        newPlayerMenuLayer.FadeTarget = 0;
    }

    private void onNewPlayerCancel(object sender, EventArgs e)
    {
        //Fade the new player menu
        playerMenu.Unlock();
        playerMenuLayer.FadeTarget = TransformedObject.MaxAlpha;

        newPlayerMenu.Lock();
        newPlayerMenuLayer.FadeTarget = 0;
    }

    private void onPlayerCancel(object sender, EventArgs e)
    {
        playerMenuLayer.FadeTarget = 0;
        playerMenu.Lock();

        startMenuLayer.FadeTarget = TransformedObject.MaxAlpha;
        startMenu.Unlock();

        stars.MoveTo(startMenuPos);
    }

    private void onNewGame(object sender, EventArgs e)
    {
        //Fade and lock the menu
        mainMenuLayer.FadeTarget = 0;
        mainMenu.Lock();

        //Go to the selected level marker
        stars.MoveTo(markers[currentLevel].Position - centerPoint);
        updateInterface(currentLevel);
        nameFade(currentLevel, -1);

        levelSelecting = true;
    }

    private void onLoadLevel(object sender, EventArgs e)
    {
        mainMenuLayer.FadeTarget = 100;
        mainMenu.Lock();

        levelMenuLayer.FadeTarget = TransformedObject.MaxAlpha;
        levelMenu.Unlock();
    }

    private void onOptions(object sender, EventArgs e)
    {
        mainMenuLayer.FadeTarget = 100;
        mainMenu.Lock();

        optionMenuLayer.FadeTarget = TransformedObject.MaxAlpha;
        optionMenu.Unlock();
    }

    private void onCredits(object sender, EventArgs e)
    {
        mainMenuLayer.FadeTarget = 100;
        mainMenu.Lock();

        creditsMenuLayer.FadeTarget = TransformedObject.MaxAlpha;
        creditsMenu.Unlock();
    }

    private void onExit(object sender, EventArgs e)
    {
        Exit();
    }

    private void onOptionsOk(object sender, EventArgs e)
    {
        optionMenuLayer.FadeTarget = 0;
        optionMenu.Lock();

        mainMenuLayer.FadeTarget = TransformedObject.MaxAlpha;
        mainMenu.Unlock();
    }

    private void onCreditsOk(object sender, EventArgs e)
    {
        creditsMenuLayer.FadeTarget = 0;
        creditsMenu.Lock();

        mainMenuLayer.FadeTarget = TransformedObject.MaxAlpha;
        mainMenu.Unlock();
    }

    private void onLevelCancel(object sender, EventArgs e)
    {
        levelMenuLayer.FadeTarget = 0;
        levelMenu.Lock();

        mainMenuLayer.FadeTarget = TransformedObject.MaxAlpha;
        mainMenu.Unlock();
    }

    private void onSignOut(object sender, EventArgs e)
    {
        mainMenuLayer.FadeTarget = 0;
        mainMenu.Lock();

        playerMenuLayer.FadeTarget = TransformedObject.MaxAlpha;
        playerMenu.Unlock();

        stars.MoveTo(playerMenuPos);
    }

    private void onDelete(object sender, EventArgs e)
    {
        //Go to the delete menu
        optionMenuLayer.FadeTarget = 100;
        optionMenu.Lock();

        deleteMenuLayer.FadeTarget = TransformedObject.MaxAlpha;
        deleteMenu.Unlock();

        //Focus on cancel by default
        deleteMenu.Select(1);
    }

    private void onDeleteOk(object sender, EventArgs e)
    {
        //Delete the player
        players.Remove(player);
        playerMenu.RemoveElement(playerButton);

        //Write the change to file
        writePlayers();

        //Go back to the player menu
        deleteMenuLayer.FadeTarget = 0; //Fade out delete menu
        deleteMenu.Lock();

        optionMenuLayer.FadeTarget = 0; //Fade out options menu
        optionMenu.Lock();

        mainMenuLayer.FadeTarget = 0;       //Fade out main menu
        mainMenu.Lock();

        playerMenuLayer.FadeTarget = TransformedObject.MaxAlpha;    //Fade in player menu
        playerMenu.Unlock();

        stars.MoveTo(playerMenuPos);    //Move to player menu
    }

    private void onDeleteCancel(object sender, EventArgs e)
    {
        //Return to the options menu
        deleteMenuLayer.FadeTarget = 0;
        deleteMenu.Lock();

        optionMenuLayer.FadeTarget = TransformedObject.MaxAlpha;
        optionMenu.Unlock();
    }

    private void onGhostOk(object sender, EventArgs e)
    {
        //Check that a valid name has been given
        if (ghostText.Text.Length == 0 || !ghostText.DefaultCleared) return;

        //Check if a file with that name already does. If it does, and it's for another level, inform that level not to use that ghost file anymore.
        if (File.Exists(Path.Combine(AppContext.BaseDirectory, "ghosts") + Path.DirectorySeparatorChar + ghostText.Text + ".ghost"))
        {
            //Get the file's level hash
            StreamReader ghostReader = new StreamReader(Path.Combine(AppContext.BaseDirectory, "ghosts") + Path.DirectorySeparatorChar + ghostText.Text + ".ghost");
            String levelHash = ghostReader.ReadLine().Substring(1);

            //If it's a different level
            if (levelHash != playLevel.Hash)
            {
                //Tell that level that this file no longer relates to it
                ((Level)levels[levelHash]).RemoveGhost(ghostText.Text);
            }

            //Close the reader to prevent usage conflicts
            ghostReader.Close();
        }

        //Save the ghost
        Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "ghosts"));
        StreamWriter ghostWriter = new StreamWriter(Path.Combine(AppContext.BaseDirectory, "ghosts") + Path.DirectorySeparatorChar + ghostText.Text + ".ghost", false);
        ghostWriter.Write("#" + playLevel.Hash + "\n" + recorder.Compile());    //Write the level's hash, then the instruction set
        ghostWriter.Close();
        recorder = null;

        //Add it to the level
        playLevel.InsertGhost(ghostText.Text);

        //Hide the menu away
        ghostMenu.Lock();
        ghostMenuLayer.FadeTarget = 0;

        //Go back to level selecting
        levelSelecting = true;
    }

    private void onGhostCancel(object sender, EventArgs e)
    {
        recorder = null;

        ghostMenu.Lock();
        ghostMenuLayer.FadeTarget = 0;

        //Go back to level selecting
        levelSelecting = true;
    }

    private void onGhostView(object sender, EventArgs e)
    {
        levelSelecting = true;
        playingMainGame = true;
        viewGhost = (String)((DataButton)sender).Data;
        play(gameLevels[currentLevel]);
    }

    private void onViewCancel(object sender, EventArgs e)
    {
        viewMenuLayer.FadeTarget = 0;
        viewMenu.Lock();

        gameMenuLayer.FadeTarget = TransformedObject.MaxAlpha;
        gameMenu.Unlock();
    }

    private void showPauseMenu()
    {
        pauseMenuLayer.FadeTarget = TransformedObject.MaxAlpha;
        pauseMenu.Unlock();
        paused = true;
    }

    private void onContinue(object sender, EventArgs e)
    {
        pauseMenuLayer.FadeTarget = 0;
        pauseMenu.Lock();
        paused = false;
    }

    private void onQuitToMenu(object sender, EventArgs e)
    {
        oldLevel = currentLevel;
        setGameState(GameState.Menu);
        paused = false;
    }

    private void onRestart(object sender, EventArgs e)
    {
        setGameState(GameState.Main);
        paused = false;
    }

    private void onErrOk(object sender, EventArgs e)
    {
        errOkButton.Press -= new EventHandler(onErrOk);
        errMenu.Lock();
        root.Remove(errMenuLayer);
        activeMenu.Unlock();
        paused = false;
    }

    private void onMusicToggle(object sender, EventArgs e)
    {
        //Toggle music, update button text and play or stop accordingly
        if (musicOn = !musicOn)
        {
            currentSong.Play();
            musicButton.Text = "Music: on";
        }
        else
        {
            currentSong.Stop();
            musicButton.Text = "Music: off";
        }

        saveConfig();
    }

    private void onFXToggle(object sender, EventArgs e)
    {
        if (FXOn = !FXOn)
            fxButton.Text = "FX: on";
        else fxButton.Text = "FX: off";

        saveConfig();
    }

    private void onDifficultyToggle(object sender, EventArgs e)
    {
        //Toggle difficulty and update button text
        if (player.Easy = !player.Easy) difficultyButton.Text = "Difficulty: easy"; else difficultyButton.Text = "Difficulty: normal";
        writePlayers();
        setupMarkers();
    }
    #endregion

    #region input events

    private void onKeyDown(object sender, KeyEventArgs e)
    {
        /*
			//DEBUG
			if (e.KeyCode==Keys.F1) {
				menuEnd();	//VERY FRAGILE
				gameState=GameState.None;
			} else if (e.KeyCode==Keys.F2) {
				menuEnd();
				menuBegin();
			} else if (e.KeyCode==Keys.F3) {
				mainEnd();
				gameState=GameState.None;
			} else if (e.KeyCode==Keys.F4) {
				fleckoEnd();
				gameState=GameState.None;
			} else if (e.KeyCode==Keys.F5) {
				System.Diagnostics.Trace.WriteLine(device.AvailableTextureMemory);
			} else if (e.KeyCode==Keys.F6) {
				//DRAW EVERY MAP
				float scale=0.75f;
				float size=scale*Map.PreviewSize;
				int rowSize=6;
				for (int i=0;i<gameLevels.Length;i++) {
					SpriteObject map=new SpriteObject(device,new SpriteTexture(device,Map.RenderPreview(gameLevels[i].GameMap,blankTex,device,this),"0,0,"+Map.PreviewSize+","+Map.PreviewSize,Map.PreviewSize,Map.PreviewSize));
					map.XScale=map.YScale=scale;
					map.X=(i%rowSize)*size;
					map.Y=i/rowSize*size;
					root.Add(map);
				}
			} else if (e.KeyCode==Keys.F7) error("OH SHITS!");
			*/

        //Escape: toggle the pause menu in-game, navigate back (cancel) in menus, skip the intro
        if (e.KeyCode == Keys.Escape)
        {
            switch (gameState)
            {
                case GameState.Main:
                    if (dead) break;    //The death menu owns input
                    if (paused) onContinue(this, EventArgs.Empty);
                    else if (!finished) showPauseMenu();
                    break;
                case GameState.Menu:
                    controller.Press(Hoki.Controls.B);
                    break;
                case GameState.Flecko:
                    fleckoTimeLeft = 0;
                    break;
            }
        }

        //Quick restart during gameplay
        if (e.KeyCode == Keys.R && gameState == GameState.Main) onRestart(this, EventArgs.Empty);
    }

    #endregion

    #region file/stream handling
    /// <summary>
    /// Returns a stream of an embedded resource
    /// </summary>
    public System.IO.Stream getStream(string resourcePath)
    {
        return GetType().Module.Assembly.GetManifestResourceStream(resourcePath);
    }

    /// <summary>
    /// Creates a SpriteTexture from a SizeTexture and an embedded datafile
    /// </summary>
    /// <param name="tex">A SizeTexture containing the Direct3D Texture to use</param>
    /// <param name="dataPath">Path to a datafile describing how the SpriteTexture should use the image (see the SpriteTexture documentation for more information)</param>
    /// <returns></returns>
    public SpriteTexture loadTexture(SizeTexture tex, string dataPath)
    {
        return new SpriteTexture(device, tex.Tex, readFile(dataPath), tex.Width, tex.Height);
    }

    /// <summary>
    /// Creates a new Texture from an embedded resource, wrapped in a SizeTexture that contains the image's dimensions
    /// </summary>
    /// <param name="imgStream">Path to the image file</param>
    /// <returns></returns>
    public SizeTexture loadSizeTexture(String imagePath)
    {
        using (System.IO.Stream imgStream = getStream(imagePath))
        {
            Texture2D tex = Texture2D.FromStream(device, imgStream);
            return new SizeTexture(tex, tex.Width, tex.Height);
        }
    }

    /// <summary>
    /// Gets the contents of an embedded resource
    /// </summary>
    private String readFile(string resourcePath)
    {
        StreamReader sr = new StreamReader(getStream(resourcePath));
        return sr.ReadToEnd();
    }
    #endregion

    #region entry point
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
        using (Game game = new Game(args))
        {
            game.Run();
        }
    }
    #endregion

    #region writers

    private void saveConfig()
    {
        StreamWriter writer = new StreamWriter("config");
        writer.WriteLine(aName + ":" + (int)controller.GetKey(Hoki.Controls.A));
        writer.WriteLine(bName + ":" + (int)controller.GetKey(Hoki.Controls.B));
        writer.WriteLine(leftName + ":" + (int)controller.GetKey(Hoki.Controls.Left));
        writer.WriteLine(rightName + ":" + (int)controller.GetKey(Hoki.Controls.Right));
        writer.WriteLine(upName + ":" + (int)controller.GetKey(Hoki.Controls.Up));
        writer.WriteLine(downName + ":" + (int)controller.GetKey(Hoki.Controls.Down));
        writer.WriteLine(startName + ":" + (int)controller.GetKey(Hoki.Controls.Start));
        writer.WriteLine(volumeName + ":" + (int)Song.Volume);
        writer.WriteLine(musicName + ":" + (musicOn ? 1 : 0));
        writer.WriteLine(firstTimeName + ":" + 0);
        writer.WriteLine(windowedName + ":" + (windowed ? 1 : 0));
        writer.WriteLine(fxName + ":" + (FXOn ? 1 : 0));
        writer.Close();
    }

    private void writeScores()
    {
        //TODO: I bet this can be more efficient
        IEnumerator levelList = levels.Values.GetEnumerator();
        String scores = "";
        while (levelList.MoveNext())
        {
            Level l = (Level)levelList.Current;
            scores += ">" + l.Hash + "\n";
            foreach (Score s in l.TopScores)
            {
                if (s != null)
                    if (s.Name.Length != 0) //0 length name=placeholder score, not a real one
                        scores += s.Name + ":" + s.Time + ":" + (s.Perfect ? "1" : "0") + ":" + (s.Easy ? "1" : "0") + "\n";
            }
        }

        StreamWriter scoreWriter = new StreamWriter(highScoreFile, false);
        scoreWriter.Write(scores);
        scoreWriter.Close();
    }

    private void writePlayers()
    {
        StreamWriter playerWriter = new StreamWriter(userFile, false);
        foreach (Player p in players) playerWriter.WriteLine(p);
        playerWriter.Close();
    }

    #endregion

    #region control
    private void onControlDown(object sender, ControlEventArgs e)
    {
        switch (gameState)
        {
            case GameState.Main:
                switch (e.Control)
                {
                    case Hoki.Controls.A:
                        if (finished && !paused && !dead) setGameState(GameState.Menu); //Dead: wait for the death menu, which handles A itself
                        break;
                    case Hoki.Controls.Start:
                        if (finished && !paused && !dead) setGameState(GameState.Menu);
                        else if (!paused && !dead) showPauseMenu();
                        break;
                }
                break;
            case GameState.Flecko:
                fleckoTimeLeft = 0;
                break;
            case GameState.Menu:
                //Fade the controls out
                controlFader.FadeTarget = 0;
                controlTimeLeft = showControlTime;

                if (levelSelecting)
                {
                    switch (e.Control)
                    {
                        case Hoki.Controls.Left:
                            if (levelSwitch < 0)
                            {
                                levelSwitch = levelSwitchTime;
                                int oldLevel = currentLevel;    //Temp. hold the old level
                                currentLevel = Math.Max(0, currentLevel - 1);
                                nameFade(currentLevel, oldLevel);
                                updateInterface(currentLevel);
                                stars.MoveTo(markers[currentLevel].Position - centerPoint);
                            }
                            break;
                        case Hoki.Controls.Right:
                            if (levelSwitch < 0)
                            {
                                levelSwitch = levelSwitchTime;
                                int oldLevel = currentLevel;    //Temp. hold the old level
                                currentLevel = Math.Min(lastUnlockedLevel, currentLevel + 1);
                                nameFade(currentLevel, oldLevel);
                                updateInterface(currentLevel);
                                stars.MoveTo(markers[currentLevel].Position - centerPoint);
                            }
                            break;
                        case Hoki.Controls.A:
                            //Fade the game menu in
                            gameMenuLayer.FadeTarget = TransformedObject.MaxAlpha;
                            gameMenu.Unlock();
                            gameMenu.Select(0);

                            showBoxes();

                            //Switch control to the menu
                            levelSelecting = false;

                            break;
                        case Hoki.Controls.B:
                            //Return to the main menu
                            mainMenuLayer.FadeTarget = TransformedObject.MaxAlpha;
                            mainMenu.Unlock();
                            levelSelecting = false;
                            nameFade(-1, currentLevel);
                            updateInterface(-1);
                            stars.MoveTo(mainMenuPos);
                            break;
                    }
                }
                break;
        }
    }

    private string keyName(Keys key)
    {
        string text = key.ToString();

        //Clean up text
        int keyCode = (int)key;
        if (keyCode < 58 && keyCode > 46) text = "" + text[1];  //D0-D9
        else
        {
            int commaIndex = text.IndexOf(',');
            if (commaIndex >= 0) text = text.Substring(commaIndex + 2);
        }

        //Special cases
        switch (text)
        {
            case "Oemcomma":
                text = ",";
                break;
            case "OemPeriod":
                text = ".";
                break;
            case "OemQuestion":
                text = "/";
                break;
            case "OemQuotes":
                text = "\"";
                break;
            case "OemOpenBrackets":
                text = "[";
                break;
            case "OemCloseBrackets":
                text = "]";
                break;
            case "OemPipe":
            case "OemBackslash":
                text = "\\";
                break;
            case "OemMinus":
                text = "-";
                break;
            case "Oemplus":
                text = "=";
                break;
            case "Oemtilde":
                text = "~";
                break;
            case "OemSemicolon":
                text = ";";
                break;
            case "Back":
                text = "Back";
                break;
            case "Prior":
                text = "PgUp";
                break;
            case "Next":
                text = "PgDn";
                break;
            case "Divide":
                text = "/";
                break;
            case "Multiply":
                text = "*";
                break;
            case "Subtract":
                text = "-";
                break;
            case "Add":
                text = "+";
                break;
            case "Decimal":
                text = ".";
                break;
            case "Capital":
                text = "Caps";
                break;
        }

        return text;
    }

    private int nameIndex(int levelIndex)
    {
        return levelIndex > 4 ? (levelIndex < 26 ? (levelIndex - 2) / 3 : 7) : 0;
    }

    private int numberIndex(int levelIndex)
    {
        return levelIndex > 4 ? (levelIndex < 26 ? (levelIndex - 5) % 3 : 3) : levelIndex;
    }

    private void nameFade(int newIndex, int oldIndex)
    {
        //Fade the old
        if (oldIndex >= 0)
        {
            planetNames[nameIndex(oldIndex)].FadeTarget = 0;
            planetNumbers[numberIndex(oldIndex)].FadeTarget = 0;
        }

        if (newIndex >= 0)
        {
            //Position and the new
            Fader name = planetNames[nameIndex(newIndex)], number = planetNumbers[numberIndex(newIndex)];

            name.X = (this.width - (name.Width + 25)) / 2;
            number.X = name.X + name.Width + 5;

            //Fade the new in
            name.FadeTarget = number.FadeTarget = TransformedObject.MaxAlpha;
        }
    }

    private void showBoxes()
    {
        //Update the map preview
        previewTex = Map.RenderPreview(gameLevels[currentLevel].GameMap, blankTex, device, this);
        mapPreview = new SpriteObject(device, new SpriteTexture(device, previewTex, "0,0," + Map.PreviewSize + "," + Map.PreviewSize, Map.PreviewSize, Map.PreviewSize));
        mapPreview.X = previewBox.X + (previewBox.Width - mapPreview.Width) / 2;
        mapPreview.Y = previewBox.Y + (previewBox.Height - mapPreview.Height) / 2;

        //Fade the boxes in
        boxTime = boxFadeDelay;
        boxesIn = false;
    }

    private void hideBoxes()
    {
        if (previewTex != null)
        { //Get rid of the preview
            if (boxesIn != false) boxLayer.Remove(mapPreview);
            previewTex = null;
            mapPreview = null;
        }

        //Fade the boxes out
        boxLayer.FadeTarget = 0;
        boxesIn = true;
    }

    private void updateInterface(int level)
    {
        //Clear out the existing interface
        highScoreText.Text = "";    //Remove the high scores
        bestTimeText.Text = "";

        //-1 indicates that the interface should be hidden
        if (level == -1) return;

        //Update the scores
        Score[] scores = gameLevels[level].TopScores;
        for (int i = 0; i < scores.Length; i++) if (scores[i].Name.Length > 0) highScoreText.Text += scores[i].Name + " " + scores[i].TimeString + "\n";

        Score playerBest = player.GetScore(gameLevels[level].Hash);
        bestTimeText.Text = player.Name + " " + (playerBest != null ? playerBest.TimeString : "-- : --");
    }
    #endregion

    #region Heli events
    private void onStart(object sender, EventArgs e)
    {
        started = true;
    }

    private void onFinish(object sender, EventArgs e)
    {
        //Mark that the level is complete
        finished = true;

        //Store this as the old level
        oldLevel = currentLevel;

        Score score = new Score(player.Name, (int)(gameTime * 1000), heli.Perfect, player.Easy);

        //Invincible runs never record scores
        if (!Heli.Invincible)
        {
            //Insert the score into the main list if not in easy mode
            if (!player.Easy) if (playLevel.InsertScore(score)) writeScores();

            //Give the player the score
            player.AddScore(playLevel.Hash, score);
            writePlayers();
        }

        if (playingMainGame)
        {
            //If this is the newest level, unlock another
            if (currentLevel == lastUnlockedLevel)
            {
                int oldLast = lastUnlockedLevel;
                lastUnlockedLevel = Math.Min(lastUnlockedLevel + 1, gameLevels.Length - 1);
                checkSecretLock();

                //Automatically move to the next level
                currentLevel = Math.Min(lastUnlockedLevel, currentLevel + 1);
            }
        }

        //Delay input briefly
        keyDelay = 0.4f;
    }

    private void checkSecretLock()
    {
        if (lastUnlockedLevel == gameLevels.Length - 1 && player.Perfects < gameLevels.Length - 1) lastUnlockedLevel = gameLevels.Length - 2;
    }

    private void onDie(object sender, EventArgs e)
    {
        //Mark that the level is complete, and we are dead.
        finished = dead = true;

        //Store this as the old level
        oldLevel = currentLevel;

        //Get rid of the heli and make an explosion
        map.Remove(heli);
        map.Explode(heli.Position);

        //Start the timer until the game automatically returns to menu
        deadTime = maxDeadTime;
    }

    private void onHit(object sender, HitEventArgs e)
    {
        if (e.Hurt) gameTime += hitPenalty;
    }
    #endregion

    private void onEnd(object sender, EventArgs e)
    {
        endMenu.Lock();

        startMenuLayer.FadeTarget = TransformedObject.MaxAlpha;
        startMenu.Unlock();

        stars.MoveTo(startMenuPos);
    }
}
