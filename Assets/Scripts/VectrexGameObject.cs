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

        return 0;
    }

    // Emulator draw line callback
    int AddLines(int x1, int y1, int x2, int y2, int color) {
        m_index++;

        GameObject obj = null;
        LineRenderer lineRenderer = null;

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
            lineRenderer.startWidth = 1.5f;
            lineRenderer.endWidth = 1.5f;
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
        m_vectrex = new EmulatorVectrex();
        m_vectrex.m_drawingCallback = AddLines;
        m_vectrex.m_postRenderCallback = PostRender;

        m_lastButtons = new bool[]{ false, false, false, false, false, false, false, false };
        m_buttons = new bool[]{ false, false, false, false, false, false, false, false };

        m_vectrex.Init(310, 410);
        m_vectrex.Start(romName, cartridgeName);
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

    }
    
}