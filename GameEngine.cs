using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace GameEngine {

    public class Resource {

        protected static Form GameForm;

        public static void SetForm(Form GameForm) {
            Resource.GameForm = GameForm;
        }

        private String File, Name;

        public Resource(String File) {
            String CWD = GameEngine.GetWorkspace();
            this.File = (CWD + (@"\") + File);
            String[] Args = File.Split('\\');
            Args = Args[Args.Length - 1].Split('.');
            this.Name = Args[0];
        }

        public String GetName() { return Name; }
        public String GetFile() { return File; }

    }

    public class Sound : Resource {

        private System.Windows.Media.MediaPlayer MediaPlayer;

        public Sound(String File) : base(File) { }

        public void Initialize() {
            GameForm.Invoke(new Action(delegate () {
                MediaPlayer = new System.Windows.Media.MediaPlayer();
                MediaPlayer.Open(new Uri(this.GetFile()));
            }));
        }

        public System.Windows.Media.MediaPlayer GetSoundPlayer() {
            return MediaPlayer;
        }

        public void SetVolume(double Volume) {
            MediaPlayer.Volume = Volume;
        }

        public double GetVolume() {
            return MediaPlayer.Volume;
        }

    }

    public class Music : Resource {

        private System.Media.SoundPlayer SoundPlayer;

        public Music(String File) : base(File) {
            SoundPlayer = new System.Media.SoundPlayer();
            SoundPlayer.SoundLocation = (this.GetFile());
        }

        public void StartToPlay() {
            SoundPlayer.PlayLooping();
        }

        public void StopToPlay() {
            SoundPlayer.Stop();
        }

    }

    public class Sprite : Resource {

        private float AnimationSpeed = 0.0F;
        private float AnimationIndex = 0.0F;

        private float MaxAnimationIndex;
        private Bitmap Bitmap;

        private bool AnimationOnUpdate = false;

        public void SetAnimationOnUpdate(bool State) {
            AnimationOnUpdate = State;
        }

        public bool IsAnimationOnUpdate() {
            return AnimationOnUpdate;
        }

        public Sprite(String File) : base(File) {
            Bitmap = new Bitmap(this.GetFile());
            MaxAnimationIndex = (Bitmap.Width / Bitmap.Height);
        }

        public Bitmap CutByIndex(int Index) {
            Bitmap Output = new Bitmap(Bitmap.Height, Bitmap.Height);
            for(short x = 0; x < Output.Height; x++)
                for (short y = 0; y < Output.Height; y++) {
                    Output.SetPixel(x, y, Bitmap.GetPixel(Output.Height * Index + x, y));
                }
            return Output;
        }

        public void Render(Graphics GL, float x, float y, float w, float h) {
            if (AnimationSpeed <= 0.0F) GL.DrawImage(Bitmap, x, y, w, h);
            else GL.DrawImage(CutByIndex((int)Math.Floor(AnimationIndex)), x, y, w, h);
        }

        public void SetAnimationSpeed(float Speed) {
            AnimationSpeed = Speed;
        }

        public void UpdateAnimation() {
            AnimationIndex += AnimationSpeed;
        }

    }

    public class Windows32 {

        [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")] static extern Int32 ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")] static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        static public Color GetPixelColor(IntPtr Handle, int x, int y) {
            IntPtr hdc = GetDC(Handle);
            uint pixel = GetPixel(hdc, x, y);
            ReleaseDC(IntPtr.Zero, hdc);
            Color color = Color.FromArgb(
                (int) (pixel & 0x000000FF),
                (int) (pixel & 0x0000FF00) >> 8,
                (int) (pixel & 0x00FF0000) >> 16
            );
            return color;
        }

    }

    public class Direction {

        public static Direction HORIZONTAL = new Direction(0);
        public static Direction VERTICAL = new Direction(1);
        public static Direction WEST = new Direction(0);
        public static Direction EAST = new Direction(0);
        public static Direction NORTH = new Direction(1);
        public static Direction SOUTH = new Direction(1);

        private int Index;

        private Direction(int Index) {
            this.Index = Index;
        }

    }

    public class Renderer {

        public static Color MixColor(Color First, Color Second) {
            return Color.FromArgb (
                (First.R + Second.R) / 2,
                (First.G + Second.G) / 2,
                (First.B + Second.B) / 2
            );
        }

        public static Color GrayShade(Color Color) {
            int shade = (Color.R + Color.G + Color.B) / 3;
            return Color.FromArgb(shade, shade, shade);
        }

        public static void DrawCentereredString(Graphics GL, String Text, Brush Brush, Font Font, float x, float y) {
            x -= (Font.Size * Text.Length / 2);
            y -= (Font.Height / 2);
            GL.DrawString(Text, Font, Brush, x, y);
        }

        public static void DrawBlurRect(Graphics GL, GameEngine Engine, int Intensity, int rx, int ry, int rw, int rh) {
            Bitmap Bitmap = new Bitmap(rw, rh);
            for(int x = rx; x < rw; x++) for (int y = ry; y < rh; y++) {
                    Color Color = Engine.GetPixelColor(x, y);
                    for (int i = 0; i < Intensity; i++) {
                        if (x - i >= 0) Color = MixColor(Color, Engine.GetPixelColor(x - i, y));
                        if (x + i < Engine.GetResolution().Width) Color = MixColor(Color, Engine.GetPixelColor(x + i, y));
                        if (y - i >= 0) Color = MixColor(Color, Engine.GetPixelColor(x, y - i));
                        if (y + i < Engine.GetResolution().Height) Color = MixColor(Color, Engine.GetPixelColor(x, y + i));
                    }
                    Bitmap.SetPixel(x - rx, y - ry, Color);
            }
            GL.DrawImage(Bitmap, rx, ry);
        }

    }

    public interface GameTickListener {
        void UpdateTick();
        void RenderTick(Graphics GL);
    }

    public class GUIComponent : GameTickListener {

        public RectangleF Aligner, Area;

        public GUIComponent(RectangleF Definer) {
            Aligner = Definer;
            Area = Aligner;
        }

        public void PreUpdateTick(Size Resolution) {
            Area.X = (Resolution.Width * Aligner.X);
            Area.Y = (Resolution.Height * Aligner.Y);
            Area.Width = (Resolution.Width * Aligner.Width);
            Area.Height = (Resolution.Height * Aligner.Height);
        }

        public virtual void Initialize() { }
        public virtual void Dispose() { }
        public virtual void RenderTick(Graphics GL) { }
        public virtual void UpdateTick() { }
    }

    public class GameGUI : List<GUIComponent>, GameTickListener {

        public GameEngine Engine;
        public Form Game;
        public Size Resolution; 

        public virtual void Initialize() { }
        public virtual void Dispose() { }
        public virtual void RenderTick(Graphics GL) { }
        public virtual void UpdateTick() { }
    }

    public class GameEngine : List<GameTickListener> {

        private static String Workspace = Application.StartupPath;

        private const int SECOND = 1000;

        private List<Keys> HoldKeys = new List<Keys>();
        private List<MouseButtons> HoldButtons = new List<MouseButtons>();

        private List<Resource> Resources = new List<Resource>();

        private Form GameForm;
        private Thread Updater, Renderer, ControlThread;
        private int TPS = 24, FPS = 60;
        private int DebugFPS = 0, DebugTPS = 0;
        private int Tickrate, Framerate;
        private Size Resolution = new Size(1280, 720);

        private Brush BackBrush;
        private bool Running = false;
        private bool Fullscreen = false;
        private bool ThreadSleep = false;
        private GameGUI GUI;

        public void Initialize(Form GameForm) {
            this.GameForm = GameForm;
            Resource.SetForm(GameForm);
            this.GameForm.Size = Resolution;
            this.GameForm.Paint += new PaintEventHandler(Paint);
            this.GameForm.FormClosing += new FormClosingEventHandler(GameClosing);
            this.GameForm.Text += (" | ") + ("Made with Effyiex's Game Engine");
            this.Updater = new Thread(Update);
            this.Renderer = new Thread(Render);
            this.BackBrush = new SolidBrush(GameForm.BackColor);
            typeof(Form).InvokeMember(("DoubleBuffered"), BindingFlags.SetProperty
                | BindingFlags.Instance | BindingFlags.NonPublic,
                null, GameForm, new object[] { true });
            this.GameForm.KeyDown += new KeyEventHandler(KeyDown);
            this.GameForm.KeyUp += new KeyEventHandler(KeyUp);
            this.GameForm.MouseDown += new MouseEventHandler(MouseDown);
            this.GameForm.MouseUp += new MouseEventHandler(MouseUp);
            this.SetTickrate(TPS);
            this.SetFramerate(FPS);
        }

        #region "Act: Resource Management"

        public void SetIcon(String File) {
            Resource Resource = new Resource(File);
            Bitmap Bitmap = new Bitmap(Resource.GetFile());
            GameForm.Icon = Icon.FromHandle(Bitmap.GetHicon());
        }

        public void PlaySound(String Name) {
            foreach (Resource Resource in Resources) {
                if (Resource.GetType().Equals(typeof(Sound)))
                    if (Resource.GetName().ToLower().Equals(Name.ToLower()))
                        GameForm.Invoke(new Action(delegate () {
                            Sound Sound = (Resource as Sound);
                            Sound.GetSoundPlayer().Open(new Uri(Resource.GetFile()));
                            Sound.GetSoundPlayer().Play();
                        }));
            }
        }

        public void Add(Resource Resource) {
            Resources.Add(Resource);
        }

        public void Remove(Resource Resource) {
            Resources.Remove(Resource);
        }

        public void LoadAllResources() {
            foreach (Resource Resource in Resources)
                if (Resource.GetType().Equals(typeof(Sound)))
                    (Resource as Sound).Initialize();
        }

        public Resource GetResource(String Name, Type Type) {
            foreach (Resource Resource in Resources) {
                if (Resource.GetName().ToLower().Equals(Name.ToLower()))
                    if(Resource.GetType().Equals(Type))
                        return Resource;
            }
            return null;
        }

        #endregion

        #region "Act: Canvas Painting"

        private void Paint(object sender, PaintEventArgs args) {
            Graphics GL = (args.Graphics as Graphics);
            GL.FillRectangle(BackBrush, 0, 0, GameForm.Width, GameForm.Height);
            GL.ScaleTransform((float)GameForm.Width / (float)Resolution.Width, (float)GameForm.Height / (float)Resolution.Height);
            try {
                foreach (GameTickListener Listener in this as GameEngine) Listener.RenderTick(GL);
            } catch (InvalidOperationException) { }
        }

        #endregion

        #region "Act: Input Handling"

        private void KeyDown(object sender, KeyEventArgs args) {
            if (!IsKeyDown(args.KeyCode)) HoldKeys.Add(args.KeyCode);
            if (!args.Handled && args.KeyCode.Equals(Keys.F11)) ToggleFullscreen();
        }

        private void KeyUp(object sender, KeyEventArgs args) {
            if (IsKeyDown(args.KeyCode)) HoldKeys.Remove(args.KeyCode);
        }

        private void MouseDown(object sender, MouseEventArgs args) {
            if (!IsButtonDown(args.Button)) HoldButtons.Add(args.Button);
        }

        private void MouseUp(object sender, MouseEventArgs args) {
            if (IsButtonDown(args.Button)) HoldButtons.Remove(args.Button);
        }

        #endregion

        #region "Act: Simple Info Getters"

        public static String GetWorkspace() {
            return GameEngine.Workspace;
        }

        public int GetDebugFPS() {
            return DebugFPS;
        }

        public int GetDebugTPS() {
            return DebugTPS;
        }

        public Size GetResolution() {
            return Resolution;
        }

        public Color GetPixelColor(int x, int y) {
            var screenCoords = GameForm.PointToScreen(new Point(x, y));
            return Windows32.GetPixelColor(GameForm.Handle, screenCoords.X, screenCoords.Y);
        }

        public bool IsKeyDown(Keys Key) {
            return HoldKeys.Contains(Key);
        }

        public bool IsButtonDown(MouseButtons Button) {
            return HoldButtons.Contains(Button);
        }

        #endregion

        #region "Act: Game-State Handling"

        public void Display(GameGUI GUI) {
            SetThreadsSleeping(true);
            if (this.GUI != null) {
                foreach (GUIComponent Component in this.GUI) {
                    Component.Dispose();
                    this.Remove(Component);
                }
                this.GUI.Dispose();
                this.Remove(this.GUI);
            }
            GUI.Engine = this;
            GUI.Game = GameForm;
            this.GUI = GUI;
            if (this.GUI != null) {
                foreach (GUIComponent Component in this.GUI) {
                    Component.Initialize();
                    this.Add(Component);
                }
                this.GUI.Initialize();
                this.Add(this.GUI);
            }
            SetThreadsSleeping(false);
        }

        public void StartGame() {
            this.Running = true;
            this.Updater.Start();
            this.Renderer.Start();
        }

        private void GameClosing(object sender, FormClosingEventArgs e) {
            if(e.CloseReason.Equals(CloseReason.UserClosing)) {
                this.StopGame();
                e.Cancel = true;
            }
        }

        public void Centerize() {
            Rectangle Bounds = Screen.FromControl(GameForm).Bounds;
            Bounds.Width /= 2; Bounds.Height /= 2;
            Bounds.X += Bounds.Width / 2;
            Bounds.Y += Bounds.Height / 2;
            GameForm.Bounds = Bounds;
        }

        public void ToggleFullscreen() {
            Fullscreen = !Fullscreen;
            if (Fullscreen) {
                GameForm.FormBorderStyle = FormBorderStyle.None;
                GameForm.Bounds = Screen.FromControl(GameForm).Bounds;
            } else {
                GameForm.FormBorderStyle = FormBorderStyle.Sizable;
                this.Centerize();
            }
        }

        public void StopGame() {
            this.Running = false;
            GameForm.Invoke(new Action(delegate {
                while (Renderer.IsAlive || Updater.IsAlive)
                    continue;
                Environment.Exit(0);
            }));
        }

        #endregion

        #region "Act: Setting-Setters"

        public void SetResolution(int Width, int Height) {
            Size Resolution = new Size(Width, Height);
            this.Resolution = Resolution;
        }

        public void SetFramerate(int FPS) {
            this.FPS = FPS;
            this.Framerate = (SECOND / FPS);
        }

        public void SetTickrate(int TPS) {
            this.TPS = TPS;
            this.Tickrate = (SECOND / TPS);
        }

        public static void SetWorkspace(Object Workspace) {
            GameEngine.Workspace = Workspace as String;
        }

        #endregion

        #region "Act: Thread Handling"

        private bool ThreadControlling = false;
        private int ControlFPS = 0, ControlTPS = 0;

        public void SetThreadsSleeping(bool State) {
            ThreadSleep = State;
        }

        public void SetThreadControl(bool State) {
            ThreadControlling = State;
            if (!State) ControlThread.Interrupt();
            else {
                ControlThread = new Thread(ControlThreading);
                ControlThread.Start();
            }
        }

        private void ControlThreading() {
            while(ThreadControlling) {
                DebugFPS = ControlFPS;
                DebugTPS = ControlTPS;
                ControlFPS = 0;
                ControlTPS = 0;
                Thread.Sleep(SECOND);
            }
        }

        private void Render() {
            while (Running) {
                while (ThreadSleep) Thread.Sleep(SECOND / FPS);
                this.GameForm.Invalidate();
                if(ThreadControlling) ControlFPS++;
                Thread.Sleep(Framerate);
            }
        }

        private void Update() {
            while(Running) {
                while (ThreadSleep) Thread.Sleep(SECOND / TPS);
                try {
                    foreach (GameTickListener Listener in this) {
                        if (Listener.GetType().Equals(typeof(GUIComponent)))
                            (Listener as GUIComponent).PreUpdateTick(Resolution);
                        Listener.UpdateTick();
                    }
                    foreach (Resource Resource in Resources) {
                        if(Resource.GetType().Equals(typeof(Sprite))) {
                            Sprite Sprite = (Resource as Sprite);
                            if (Sprite.IsAnimationOnUpdate())
                                Sprite.UpdateAnimation();
                        }
                    }
                } catch(InvalidOperationException) { }
                GUI.Resolution = Resolution;
                if (ThreadControlling) ControlTPS++;
                Thread.Sleep(Tickrate);
            }
        }

        #endregion

    }

}
