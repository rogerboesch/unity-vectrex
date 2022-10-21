#define TILT_5

using System.Collections.Generic;
using UnityEngine;
using TMPro;
using TiltFive;

public class AppController : MonoBehaviour
{
    private const int MAX_LOG_LINES = 50;

    private static AppController s_instance = null;

    public bool gameMode = true;
    public bool showLogInfo = true;
    public T5InputReceiver receiver = null;
    public TMP_Text gameNameLabel = null;
    public TMP_Text modeLabel = null;
    public VectrexRenderer vectrexRenderer = null;
    public GameObject vectrexModel = null;
    public TiltFiveManager tiltManager = null;
    public TMP_Text logTextLabel = null;
   
    private List<string> m_games = new List<string>();
    private int m_currentGameIndex = 0;
    private List<string> m_log = new List<string>();

    public static AppController Instance { get { return s_instance; } }

    // Handling of proper game name
    private string FirstCharacterToUpper(string str)
    {
        if (str == null)
            return null;

        if (str.Length > 1)
            return char.ToUpper(str[0]) + str.Substring(1);

        return str.ToUpper();
    }

    private string GetReadableName(string romName) {
        string[] parts = romName.Split('.');
        if (parts.Length > 1) {
            romName = parts[0];
        }

        string name = "";
        parts = romName.Split('_');
        foreach (var part in parts)
        {
            if (name.Length > 0) {
                name += " ";
            }

            name += FirstCharacterToUpper(part);
        } 

        return name;
    }

    // Handling of game list

    private string GetCurrentGame() {
        return m_games[m_currentGameIndex];
    }

    private void LoadPreviousGame() {
        m_currentGameIndex--;
        if (m_currentGameIndex < 0) {
            m_currentGameIndex = m_games.Count-1;
        }

        UpdateUI();

        vectrexRenderer.StartGame(GetCurrentGame());
    }

    private void LoadNextGame() {
        m_currentGameIndex++;
        if (m_currentGameIndex >= m_games.Count) {
            m_currentGameIndex = 0;
        }

        UpdateUI();

        vectrexRenderer.StartGame(GetCurrentGame());
    }

    private void CreateGameList() {
        m_games.Add("mine_storm.bin");
        m_games.Add("polar_rescue.bin");
        m_games.Add("pole_position.bin");
        m_games.Add("web_wars.bin");
        m_games.Add("armor_attack.bin");
        m_games.Add("bedlam.bin");
        m_games.Add("berzerk.bin");
        m_games.Add("blitz.bin");
        m_games.Add("clean_sweep.bin");
        m_games.Add("cosmic_chasm.bin");
        m_games.Add("fortress_of_narzord.bin");
        m_games.Add("headsup.bin");
        m_games.Add("hyperchase.bin");
        m_games.Add("rip-off.bin");
        m_games.Add("scramble.bin");
        m_games.Add("solar_quest.bin");
        m_games.Add("space_wars.bin");
        m_games.Add("spike.bin");
        m_games.Add("spinball.bin");
        m_games.Add("star_castle.bin");
        m_games.Add("star_trek.bin");
        m_games.Add("starhawk.bin");
    }

    // Input handling

    public void OnButtonOnePressed()
    {
        if (gameMode)
        {
            LoadPreviousGame();
        }
        else
        {
            ToggleLogInfo();
        }
    }

    public void OnButtonTwoPressed()
    {
        if (gameMode)
        {
            LoadNextGame();
        }
    }

    public void OnStickHeldLeft()
    {
        if (!gameMode)
        {
            RotateModel(-1.0f);
        }
    }

    public void OnStickHeldRight()
    {
        if (!gameMode)
        {
            RotateModel(1.0f);
        }
    }

    public void OnStickHeldUp()
    {
        if (!gameMode)
        {
            ChangeModelScale(0.1f);
        }
    }

    public void OnStickHeldDown()
    {
        if (!gameMode)
        {
            ChangeModelScale(-0.1f);
        }
    }

    public void OnStickPressed()
    {
        ToggleMode();
    }

    // UI

    private void UpdateUI() {
        if (gameMode) {
            modeLabel.text = "Play Mode";
        }
        else {
            modeLabel.text = "Settings";
        }

        gameNameLabel.text = GetReadableName(GetCurrentGame());
    }

    private void UpdateLogUI()
    {
        string text = "";

        foreach (var line in m_log)
        {
            if (text.Length > 0)
            {
                text += "\n";
            }

            text += line;
        }

        logTextLabel.text = text;
    }

    void ToggleMode() {
        gameMode = !gameMode;

        vectrexRenderer.PauseGame(!gameMode);

        UpdateUI();
    }

    void ToggleLogInfo()
    {
        showLogInfo = !showLogInfo;
        logTextLabel.enabled = showLogInfo;

        UpdateLogUI();
    }

    void ChangeContentSize(float value)
    {
        float currentSize = tiltManager.scaleSettings.contentScaleRatio;
        float newSize = currentSize + value;

        if (newSize < 1.0f || newSize > 10.0f)
        {
            return;
        }

        tiltManager.scaleSettings.contentScaleRatio = newSize;
    }

    void ChangeModelScale(float value)
    {
        Vector3 scale = vectrexModel.transform.localScale;
        float currentScale = scale.y;
        float newScale = currentScale + value;

        if (newScale < 1.0f || newScale > 100.0f)
        {
            return;
        }


        vectrexModel.transform.localScale = new Vector3(newScale, newScale, newScale);
    }

    void RotateModel(float value)
    {
        Vector3 rotation = vectrexModel.transform.eulerAngles;
        rotation.y += value;
        vectrexModel.transform.eulerAngles = rotation;
    }

    // Game loop

    private void Awake()
    {
        s_instance = this;
    }

    void Start()
    {
        CreateGameList();

        UpdateUI();
        
        receiver.OnOnePressed.AddListener(OnButtonOnePressed);
        receiver.OnTwoPressed.AddListener(OnButtonTwoPressed);
        receiver.OnStickHeldRight.AddListener(OnStickHeldRight);
        receiver.OnStickHeldLeft.AddListener(OnStickHeldLeft);
        receiver.OnStickHeldUp.AddListener(OnStickHeldUp);
        receiver.OnStickHeldDown.AddListener(OnStickHeldDown);
        receiver.OnStickPressed.AddListener(OnStickPressed);

        vectrexRenderer.StartGame(GetCurrentGame());
    }

    void Update()
    {
        if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
        {
            ToggleMode();
        }

        if (gameMode)
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow))
            {
                LoadPreviousGame();
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow))
            {
                LoadNextGame();
            }
        }
        else
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow))
            {
                ChangeModelScale(-1.0f);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow))
            {
                ChangeModelScale(1.0f);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow))
            {
                RotateModel(1.0f);
            }
        }
    }

    // Logging (UI)

    public void AddMessage(string text)
    {
        m_log.Add(text);

        if (m_log.Count > MAX_LOG_LINES)
        {
            m_log.RemoveAt(0);
        }

        UpdateLogUI();
    }
}
