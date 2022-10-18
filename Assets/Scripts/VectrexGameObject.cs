using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VectrexGameObject : MonoBehaviour {

    public int xOffset = 0;
    public string romName = "romfast.bin";
    public string cartridgeName = "";
    
    private EmulatorVectrex m_vectrex;

    private bool[] m_buttons;
    private bool[] m_lastButtons;

    private int m_index = 0;
    private int m_lines = 0;

    private List<string> m_games = new List<string>();
    private int m_currentGameIndex = 0;

    private void CreateGameList() {
        m_games.Add("armor_attack.bin");
        m_games.Add("bedlam.bin");
        m_games.Add("berzerk.bin");
        m_games.Add("blitz.bin");
        m_games.Add("clean_sweep.bin");
        m_games.Add("cosmic_chasm.bin");
        m_games.Add("fortress_of_narzord.bin");
        m_games.Add("headsup.bin");
        m_games.Add("hyperchase.bin");
        m_games.Add("mine_storm.bin");
        m_games.Add("polar_rescue.bin");
        m_games.Add("pole_position.bin");
        m_games.Add("rip-off.bin");
        m_games.Add("scramble.bin");
        m_games.Add("solar_quest.bin");
        m_games.Add("space_wars.bin");
        m_games.Add("spike.bin");
        m_games.Add("spinball.bin");
        m_games.Add("star_castle.bin");
        m_games.Add("star_trek.bin");
        m_games.Add("starhawk.bin");
        m_games.Add("web_wars.bin");
    }

    private string GetCurrentGame() {
        return m_games[m_currentGameIndex];
    }

    private void LoadNextGame() {
        m_currentGameIndex++;
        if (m_currentGameIndex >= m_games.Count) {
            m_currentGameIndex = 0;
        }

        m_vectrex.Start(romName, GetCurrentGame());
    }

    // Emulator post render callback
    int PostRender() {
        // Hide all game objects that left over
        GameObject obj = null;
        for (int i = m_index; i < m_lines; i++) {
            obj = transform.GetChild(i).gameObject;

            if (obj != null) {
                LineRenderer lineRenderer = obj.GetComponent<LineRenderer>();
                lineRenderer.enabled = false;
            }
        }

        // Adjust scale and position
        transform.localScale = new Vector3(0.0004f, 0.0004f, 1.0f);
        transform.localPosition = new Vector3(0.01f, 0.12f, 2.27f);

        return 0;
    }

    // Emulator draw line callback
    int AddLines(int x1, int y1, int x2, int y2, int color) {
        m_index++;

        GameObject obj = null;
        LineRenderer lineRenderer = null;

        if (x2-x1 == 0) {
            x1--;
        }
        else if (y2-y1 == 0) {
          // y2++;
        }

        if (m_index > transform.childCount) { 
            obj = new GameObject("line_"+m_index);
            lineRenderer = obj.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = false;
            Material mat = new Material(Shader.Find("Unlit/Texture"));
            lineRenderer.material = mat;
            lineRenderer.startColor = Color.white;
            lineRenderer.endColor = Color.white;
            lineRenderer.material.color = Color.white;

            // set width of the renderer
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0.01f;
        }
        else {
            obj = transform.GetChild(m_index-1).gameObject;
            lineRenderer = obj.GetComponent<LineRenderer>();
            lineRenderer.enabled = true;
        }

        // set the position
        Vector3 pos1 = new Vector3(xOffset+155-x1, 410-y1, 0);
        Vector3 pos2 = new Vector3(xOffset+155-x2, 410-y2, 0);
        lineRenderer.SetPosition(0, pos1);
        lineRenderer.SetPosition(1, pos2);

        obj.transform.SetParent(transform);
        
        return 0;
    }

    void SetButton(int key) {   
        m_buttons[key] = true;

        if (m_buttons[key] && !m_lastButtons[key]) {
            m_vectrex.Key(key, true);
        } 
    }

    void ReleaseButtons() {
        for (int key = 0; key < 8; key++) {
            if (!m_buttons[key] && m_lastButtons[key]) {
                m_vectrex.Key(key, false);
            }

            m_lastButtons[key] = m_buttons[key];
            m_buttons[key] = false;
        }
    }

    // Start is called before the first frame update
    void Start() {  
        CreateGameList();
        
        m_vectrex = new EmulatorVectrex();
        m_vectrex.m_drawingCallback = AddLines;
        m_vectrex.m_postRenderCallback = PostRender;

        m_lastButtons = new bool[]{ false, false, false, false, false, false, false, false };
        m_buttons = new bool[]{ false, false, false, false, false, false, false, false };

        m_vectrex.Init(310, 410);
        m_vectrex.Start(romName, GetCurrentGame());
    }

    // Update is called once per frame
    void Update() {    
        m_index = 0;

        // Keys are for now optimised for Minestorm. Will change that after
        if (Input.GetAxis("Horizontal") < 0) {
            SetButton(Vectrex.PL2_LEFT);
        }
        else if (Input.GetAxis("Horizontal") > 0) {
            SetButton(Vectrex.PL2_RIGHT);
        }

        if (Input.GetAxis("Vertical") < 0) {
            SetButton(Vectrex.PL1_RIGHT);
        }
        else if (Input.GetAxis("Vertical") > 0) {
            SetButton(Vectrex.PL1_UP);
        }

        if (Input.GetButton("Jump")) {
            SetButton(Vectrex.PL1_DOWN);
        }

        ReleaseButtons();

        m_vectrex.Frame();

        if (m_index > m_lines) {
            m_lines = m_index;
            Debug.LogFormat("New line max: {0}", m_lines);
        }

        if (Input.GetKeyDown(KeyCode.Escape)) {
            LoadNextGame();
        }
    }
    
}