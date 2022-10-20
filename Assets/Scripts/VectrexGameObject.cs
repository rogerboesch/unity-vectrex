#define TILT_5

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VectrexGameObject : MonoBehaviour {
    public string romName = "romfast.bin";
    public int xOffset = 0;
    public int yOffset = 0;
    public T5InputReceiver receiver = null;

    private EmulatorVectrex m_vectrex = null;
    
    private bool[] m_buttons;
    private bool[] m_lastButtons;

    private int m_index = 0;
    private int m_lines = 0;

    private bool m_paused = false;

    // Input handling

    public void OnGameButton1Pressed()
    {
        m_vectrex.Key(Vectrex.PL1_LEFT, true);
    }

    public void OnGameButton2Pressed()
    {
        m_vectrex.Key(Vectrex.PL1_RIGHT, true);
    }

    public void OnGameButton3Pressed()
    {
        m_vectrex.Key(Vectrex.PL1_UP, true);
    }

    public void OnGameButton4Pressed()
    {
        m_vectrex.Key(Vectrex.PL1_DOWN, true);
    }

    public void OnGameButton1Released()
    {
        m_vectrex.Key(Vectrex.PL1_LEFT, false);
    }

    public void OnGameButton2Released()
    {
        m_vectrex.Key(Vectrex.PL1_RIGHT, false);
    }

    public void OnGameButton3Released()
    {
        m_vectrex.Key(Vectrex.PL1_UP, false);
    }

    public void OnGameButton4Released()
    {
        m_vectrex.Key(Vectrex.PL1_DOWN, false);
    }

    public void OnGameStickLeft()
    {
        m_vectrex.Key(Vectrex.PL2_LEFT, true);
        Debug.LogFormat("Stick left");
    }

    public void OnGameStickRight()
    {
        m_vectrex.Key(Vectrex.PL2_RIGHT, true);
        Debug.LogFormat("Stick right");
    }

    public void OnGameStickUp()
    {
        m_vectrex.Key(Vectrex.PL2_UP, true);
        Debug.LogFormat("Stick up");
    }

    public void OnGameStickDown()
    {
        m_vectrex.Key(Vectrex.PL2_DOWN, true);
        Debug.LogFormat("Stick down");
    }

    public void OnGameStickReleased()
    {
        Debug.LogFormat("Stick released");

        m_vectrex.Key(Vectrex.PL2_LEFT, false);
        m_vectrex.Key(Vectrex.PL2_RIGHT, false);
        m_vectrex.Key(Vectrex.PL2_UP, false);
        m_vectrex.Key(Vectrex.PL2_DOWN, false);
    }

    // Emulator

    // Post render callback
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
        transform.localScale = new Vector3(0.0004f*1.2f, 0.0004f*1.2f, 1.0f);
        transform.localPosition = new Vector3(0.01f, 0.12f, 2.27f);

        return 0;
    }

    // Emulator draw line callback
    int AddLines(int x1, int y1, int x2, int y2, int color) {
        m_index++;

        GameObject obj = null;
        LineRenderer lineRenderer = null;

        if (x2-x1 == 0) {
            x2++;
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

        // set the position (X is wrong, must be mirrored)
        Vector3 pos1 = new Vector3(x1-xOffset, 410-y1-yOffset, 0);
        Vector3 pos2 = new Vector3(x2-xOffset, 410-y2-yOffset, 0);
        lineRenderer.SetPosition(0, pos1);
        lineRenderer.SetPosition(1, pos2);

        obj.transform.SetParent(transform);
        
        return 0;
    }

    public void StartGame(string name) {
        if (m_vectrex == null)
        {
            m_vectrex = new EmulatorVectrex();
            m_vectrex.m_drawingCallback = AddLines;
            m_vectrex.m_postRenderCallback = PostRender;

            m_lastButtons = new bool[] { false, false, false, false, false, false, false, false };
            m_buttons = new bool[] { false, false, false, false, false, false, false, false };

            m_vectrex.Init(310, 410);
        }

        m_vectrex.Start(romName, name);
    }

    public void PauseGame(bool flag)
    {
        if (m_paused == flag)
        {
            return;
        }

        m_paused = flag;
    }

    // Vectrex key handling

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

    // Game loop

    void Start() {  
        // Tilt 5 Integration (temporary)
#if TILT_5
        receiver.OnAPressed.AddListener(OnGameButton1Pressed);
        receiver.OnBPressed.AddListener(OnGameButton2Pressed);
        receiver.OnXPressed.AddListener(OnGameButton3Pressed);
        receiver.OnYPressed.AddListener(OnGameButton4Pressed);
        receiver.OnALifted.AddListener(OnGameButton1Released);
        receiver.OnBLifted.AddListener(OnGameButton2Released);
        receiver.OnXLifted.AddListener(OnGameButton3Released);
        receiver.OnYLifted.AddListener(OnGameButton4Released);

        receiver.OnStickLeft.AddListener(OnGameStickLeft);
        receiver.OnStickRight.AddListener(OnGameStickRight);
        receiver.OnStickUp.AddListener(OnGameStickUp);
        receiver.OnStickDown.AddListener(OnGameStickDown);
        receiver.OnStickStopMoving.AddListener(OnGameStickReleased);

        receiver.OnTriggerPressed.AddListener(OnGameButton4Pressed);
        receiver.OnTriggerReleased.AddListener(OnGameButton4Released);
#endif
    }

    // Update is called once per frame
    void Update() {  
        if (m_paused)
        {
            return;
        }

        m_index = 0;

#if TILT_5
#else
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
#endif

        m_vectrex.Frame();

        if (m_index > m_lines) {
            m_lines = m_index;
            Debug.LogFormat("New line max: {0}", m_lines);
        }
    }
    
}