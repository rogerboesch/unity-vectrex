#define TILT_5

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using TiltFive;

public class AppController : MonoBehaviour
{
    public bool gameMode = true;
    public T5InputReceiver receiver = null;
    public TMP_Text gameNameLabel = null;
    public TMP_Text modeLabel = null;
    public VectrexGameObject vectrexController = null;
    public GameObject vectrexModel = null;
    public TiltFiveManager tiltManager = null;

    private List<string> m_games = new List<string>();
    private int m_currentGameIndex = 0;

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

        vectrexController.StartGame(GetCurrentGame());
    }

    private void LoadNextGame() {
        m_currentGameIndex++;
        if (m_currentGameIndex >= m_games.Count) {
            m_currentGameIndex = 0;
        }

        UpdateUI();

        vectrexController.StartGame(GetCurrentGame());
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

    public void OnPreviousGameButton()
    {
        LoadPreviousGame();
    }

    public void OnNextGameButton()
    {
        LoadNextGame();
    }

    public void OnRotateLeft()
    {
        if (!gameMode)
        {
            RotateModel(-1.0f);
        }
    }

    public void OnRotateRight()
    {
        if (!gameMode)
        {
            RotateModel(1.0f);
        }
    }

    public void OnIncreaseContentSize()
    {
        if (!gameMode)
        {
            ChangeContentSize(0.1f);
        }
    }

    public void OnDecreaseContentSize()
    {
        if (!gameMode)
        {
            ChangeContentSize(-0.1f);
        }
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

    void ToggleMode() {
        gameMode = !gameMode;

        vectrexController.PauseGame(!gameMode);

        UpdateUI();
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

    void RotateModel(float value)
    {
        Vector3 rotation = vectrexModel.transform.eulerAngles;
        rotation.y += value;
        vectrexModel.transform.eulerAngles = rotation;
    }

    // Game loop

    void Start()
    {
        CreateGameList();

        UpdateUI();
        
        receiver.OnOnePressed.AddListener(OnPreviousGameButton);
        receiver.OnTwoPressed.AddListener(OnNextGameButton);
        receiver.OnStickHeldRight.AddListener(OnRotateRight);
        receiver.OnStickHeldLeft.AddListener(OnRotateLeft);
        receiver.OnStickHeldUp.AddListener(OnIncreaseContentSize);
        receiver.OnStickHeldDown.AddListener(OnDecreaseContentSize);

        vectrexController.StartGame(GetCurrentGame());
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
                ChangeContentSize(-1.0f);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow))
            {
                ChangeContentSize(1.0f);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow))
            {
                RotateModel(1.0f);
            }
        }
    }

}
