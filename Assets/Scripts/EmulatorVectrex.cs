using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

using Uint = System.Int32;
using Uint8 = System.Byte;
using Ulong = System.Int32;

public class Vectrex {
    public const Uint VECTREX_MHZ = 1500000; // speed of the vectrex being emulated
    public const Uint VECTREX_COLORS = 128;     // number of possible colors ... grayscales
    public const Uint ALG_MAX_X = 33000;
    public const Uint ALG_MAX_Y = 41000;

    public const Uint VECTREX_PDECAY = 30;                            // phosphor decay rate
    public const Uint FCYCLES_INIT = VECTREX_MHZ / VECTREX_PDECAY;    // number of 6809 cycles before a frame redraw
    public const Uint VECTOR_CNT = VECTREX_MHZ / VECTREX_PDECAY;      // max number of possible vectors that maybe on the screen at one time
    public const Uint VECTOR_HASH = 65521;

    public const Uint TIMER = 25;

    public const Uint PL1_LEFT = 0;
    public const Uint PL1_RIGHT = 1;
    public const Uint PL1_UP = 2;
    public const Uint PL1_DOWN = 3;

    public const Uint PL2_LEFT = 4;
    public const Uint PL2_RIGHT = 5;
    public const Uint PL2_UP = 6;
    public const Uint PL2_DOWN = 7;

}

public struct Vector {
    public long x0, y0;            // start coordinate
    public long x1, y1;            // end coordinate
    public Uint8 color;             // 0..VECTREX_COLORS-1

    public Vector(long x0, long y0, long x1, long y1, Uint8 color) {
        this.x0 = x0; this.y0 = y0;
        this.x1 = x1; this.y1 = y1;
        this.color = color;
    }

};

public class EmulatorVectrex {
    public System.Func<int, int, int, int, int, int> m_drawingCallback;
    public System.Func<int> m_postRenderCallback;

    private Emulator6809 m_ic6809;
    private Emulator8910 m_ic8910;

    private Ulong m_scaling = 0;
    private Ulong m_xOffset = 0;
    private Ulong m_yOffset = 0;
    private Uint m_width = 0;
    private Uint m_height = 0;
    private bool m_isInitialised = false;
    private bool m_paused = false;
    private Uint8[] m_rom;          // 8192 bytes
    private Uint8[] m_cartridge;    // 32768 bytes
    private Uint8[] m_ram;          // 1024 bytes

    // sound chip registers
    private Uint[] m_soundRegisters;        // 16 bytes
    private Uint m_soundSelect;             

    // VIA 6522 registers
    private Uint m_via_ora;
    private Uint m_via_orb;
    private Uint m_via_ddra;
    private Uint m_via_ddrb;
    private Uint m_via_t1on;  // is timer 1 on?
    private Uint m_via_t1int; // are timer 1 interrupts allowed?
    private Uint m_via_t1c;
    private Uint m_via_t1ll;
    private Uint m_via_t1lh;
    private Uint m_via_t1pb7; // timer 1 controlled version of pb7
    private Uint m_via_t2on;  // is timer 2 on?
    private Uint m_via_t2int; // are timer 2 interrupts allowed?
    private Uint m_via_t2c;
    private Uint m_via_t2ll;
    private Uint m_via_sr;
    private Uint m_via_srb;   // number of bits shifted so far
    private Uint m_via_src;   // shift counter
    private Uint m_via_srclk;
    private Uint m_via_acr;
    private Uint m_via_pcr;
    private Uint m_via_ifr;
    private Uint m_via_ier;
    private Uint m_via_ca2;
    private Uint m_via_cb2h;  // basic handshake version of cb2
    private Uint m_via_cb2s;  // version of cb2 controlled by the shift register

    // analog devices
    private Uint m_alg_rsh;   // zero ref sample and hold
    private Uint m_alg_xsh;   // x sample and hold
    private Uint m_alg_ysh;   // y sample and hold
    private Uint m_alg_zsh;   // z sample and hold
    private Uint m_alg_jch0;  // joystick direction channel 0
    private Uint m_alg_jch1;  // joystick direction channel 1
    private Uint m_alg_jch2;  // joystick direction channel 2
    private Uint m_alg_jch3;  // joystick direction channel 3
    private Uint m_alg_jsh;   // joystick sample and hold

    private Uint m_alg_compare;

    private long m_alg_dx;     // delta x
    private long m_alg_dy;     // delta y
    private long m_alg_curr_x; // current x position
    private long m_alg_curr_y; // current y position

    Uint m_alg_vectoring; // are we drawing a vector right now?
    private long m_alg_vector_x0;
    private long m_alg_vector_y0;
    private long m_alg_vector_x1;
    private long m_alg_vector_y1;
    private long m_alg_vector_dx;
    private long m_alg_vector_dy;
    Uint8 m_alg_vector_color;

    private long m_vector_draw_count;
    private long m_vector_erase_count;
    System.Collections.ArrayList m_vectors_draw;  // VECTOR_CNT
    System.Collections.ArrayList m_vectors_erase;  // VECTOR_CNT
    Ulong[] m_vector_hash;    // VECTOR_HASH

    private long m_fcycles;

    public EmulatorVectrex() {
        m_ic6809 = new Emulator6809(this);
        m_ic8910 = new Emulator8910();

        m_rom = new Uint8[8192];
        m_ram = new Uint8[1024];
        m_cartridge = new Uint8[32768];
        m_soundRegisters = new Uint[16];

        m_vectors_draw = new System.Collections.ArrayList();
        m_vectors_erase = new System.Collections.ArrayList();

        m_vector_hash = new Ulong[Vectrex.VECTOR_HASH];
    }

    // Emulator calls
    public void Key(Uint vk, bool pressed) {
     if (pressed) {
        switch (vk) {
            case Vectrex.PL1_LEFT:
                m_soundRegisters[14] &= ~0x01;
                break;
            case Vectrex.PL1_RIGHT:
                m_soundRegisters[14] &= ~0x02;
                break;
            case Vectrex.PL1_UP:
                m_soundRegisters[14] &= ~0x04;
                break;
            case Vectrex.PL1_DOWN:
                m_soundRegisters[14] &= ~0x08;
                break;
                
            case Vectrex.PL2_LEFT:
                m_alg_jch0 = 0x00;
                break;
            case Vectrex.PL2_RIGHT:
                m_alg_jch0 = 0xff;
                break;
            case Vectrex.PL2_UP:
                m_alg_jch1 = 0xff;
                break;
            case Vectrex.PL2_DOWN:
                m_alg_jch1 = 0x00;
                break;
        }
    }
    else {
        switch (vk) {
            case Vectrex.PL1_LEFT:
                m_soundRegisters[14] |= 0x01;
                break;
            case Vectrex.PL1_RIGHT:
                m_soundRegisters[14] |= 0x02;
                break;
            case Vectrex.PL1_UP:
                m_soundRegisters[14] |= 0x04;
                break;
            case Vectrex.PL1_DOWN:
                m_soundRegisters[14] |= 0x08;
                break;
                
            case Vectrex.PL2_LEFT:
                m_alg_jch0 = 0x80;
                break;
            case Vectrex.PL2_RIGHT:
                m_alg_jch0 = 0x80;
                break;
            case Vectrex.PL2_UP:
                m_alg_jch1 = 0x80;
                break;
            case Vectrex.PL2_DOWN:
                m_alg_jch1 = 0x80;
                break;
        }
    }
   }

    public void Init(Uint width, int height) {
        m_width = width;
        m_height = height;

        Uint sclx = Vectrex.ALG_MAX_X / width;
        Uint scly = Vectrex.ALG_MAX_Y / height;

        m_scaling = sclx > scly ? sclx : scly;

        m_xOffset = (width - Vectrex.ALG_MAX_X / m_scaling) / 2;
        m_yOffset = (height - Vectrex.ALG_MAX_Y / m_scaling) / 2;
    }

    public void Start(string romfile, string cartfile) {
        LoadFile(romfile, cartfile);

        m_ic8910.Start(ref m_soundRegisters); 
        
        // audio_processor_start(m_ic8910); TODO: Start Unity sound engine

        Reset();
        
        m_isInitialised = true;
    }

    public void Frame() {
        if (!m_isInitialised) {
            return;
        }
        
        if (m_paused) {
            return;
        }
        
        Emulate((Vectrex.VECTREX_MHZ / 1000) * Vectrex.TIMER);
   }

    void Stop() {
        m_ic8910.Stop();
        m_isInitialised = false;
        // audio_processor_stop(); // TODO: Stop Unity sound
    }

    void Pause() {
        m_paused = true;
    }

    void Resume() {
        m_paused = false;
    }
    private void Reset() {
        // RAM
        for (Uint r = 0; r < 1024; r++) {
            m_ram[r] = (Uint8)(r & 0xff);
        }

        for (Uint r = 0; r < 16; r++) {
            m_soundRegisters[r] = 0;
            m_ic8910.Write(r, 0);
        }

        // input buttons
        m_soundRegisters[14] = 0xff;
        m_ic8910.Write(14, 0xff);

        m_soundSelect = 0;

        m_via_ora = 0;
        m_via_orb = 0;
        m_via_ddra = 0;
        m_via_ddrb = 0;
        m_via_t1on = 0;
        m_via_t1int = 0;
        m_via_t1c = 0;
        m_via_t1ll = 0;
        m_via_t1lh = 0;
        m_via_t1pb7 = 0x80;
        m_via_t2on = 0;
        m_via_t2int = 0;
        m_via_t2c = 0;
        m_via_t2ll = 0;
        m_via_sr = 0;
        m_via_srb = 8;
        m_via_src = 0;
        m_via_srclk = 0;
        m_via_acr = 0;
        m_via_pcr = 0;
        m_via_ifr = 0;
        m_via_ier = 0;
        m_via_ca2 = 1;
        m_via_cb2h = 1;
        m_via_cb2s = 0;

        m_alg_rsh = 128;
        m_alg_xsh = 128;
        m_alg_ysh = 128;
        m_alg_zsh = 0;
        m_alg_jch0 = 128;
        m_alg_jch1 = 128;
        m_alg_jch2 = 128;
        m_alg_jch3 = 128;
        m_alg_jsh = 128;

        m_alg_compare = 0;

        m_alg_dx = 0;
        m_alg_dy = 0;
        m_alg_curr_x = Vectrex.ALG_MAX_X / 2;
        m_alg_curr_y = Vectrex.ALG_MAX_Y / 2;
        m_alg_vectoring = 0;

        m_vector_draw_count = 0;
        m_vector_erase_count = 0;
        m_vectors_draw.Clear();
        m_vectors_erase.Clear();

        m_fcycles = Vectrex.FCYCLES_INIT;

        m_ic6809.Reset();
    }

    private void Emulate(Ulong cycles) {
        Uint c, icycles;

        while (cycles > 0) {
            icycles = m_ic6809.Step(m_via_ifr & 0x80, 0);

            for (c = 0; c < icycles; c++) {
                ViaSstep0();
                AlgSstep();
                ViaSstep1();
            }

            cycles -= icycles;

            m_fcycles -= icycles;

            if (m_fcycles < 0) {
                m_fcycles += Vectrex.FCYCLES_INIT;
                
                Render();

                // everything that was drawn during this pass now enters
                m_vectors_erase.Clear();
                foreach(Vector vector in m_vectors_draw) {
                    m_vectors_erase.Add(vector);
                }
                m_vectors_draw.Clear();

                m_vector_erase_count = m_vector_draw_count;
                m_vector_draw_count = 0;
            }
        }
    }

    private void Render() {
        foreach (Vector vector in m_vectors_draw) {
            Uint8 color = (Uint8)(vector.color * 256 / Vectrex.VECTREX_COLORS);
            
            DrawLine((Uint)(m_xOffset + vector.x0 / m_scaling),
                     (Uint)(m_yOffset + vector.y0 / m_scaling),
                     (Uint)(m_xOffset + vector.x1 / m_scaling),
                     (Uint)(m_yOffset + vector.y1 / m_scaling), color);
        }

        m_postRenderCallback();

    }

    // Internals 
    private void DrawLine(int x1, int y1, int x2, int y2, Uint8 color) {
        m_drawingCallback(x1, y1, x2, y2, color);
    }

    private byte[] LoadBytes(string filename) {
        System.IO.FileStream fileStream = null;
        System.IO.BinaryReader binReader = null;
        byte[] data = null;

        try
        {
            fileStream = new System.IO.FileStream(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            binReader = new System.IO.BinaryReader(fileStream);
            data = binReader.ReadBytes((int)fileStream.Length);

            RB.Log.Message($"ROM file {filename} loaded");
        }
        catch (FileNotFoundException)
        {
            RB.Log.Error($"ROM file {filename} NOT loaded");
        }
        finally
        {
            if (binReader != null) binReader.Close();
            if (fileStream != null) fileStream.Close();
        }

        return data;
    }

    private void LoadFile(string romfile, string cartfile) {
        m_rom = LoadBytes("C:\\Users\\me\\Desktop\\test5\\Roms\\" + romfile);

        if (cartfile.Length > 0) {
            m_cartridge = LoadBytes("C:\\Users\\me\\Desktop\\test5\\Roms\\" + cartfile);
        }
    }

    // Internal bus handling
    void SndUpdate() {
        switch (m_via_orb & 0x18) {
        case 0x00:
            /* the sound chip is disabled */
            break;
        case 0x08:
            /* the sound chip is sending data */
            break;
        case 0x10:
            /* the sound chip is recieving data */

            if (m_soundSelect != 14) {
                m_soundRegisters[m_soundSelect] = m_via_ora;
                m_ic8910.Write(m_soundSelect, m_via_ora);
            }

            break;
        case 0x18:
            /* the sound chip is latching an address */

            if ((m_via_ora & 0xf0) == 0x00) {
                m_soundSelect = m_via_ora & 0x0f;
            }

            break;
        }
    }

    void AlgUpdate() {
        // update the various analog values when orb is written.
        switch (m_via_orb & 0x06) {
        case 0x00:
            m_alg_jsh = m_alg_jch0;

            if ((m_via_orb & 0x01) == 0x00) {
                /* demultiplexor is on */
                m_alg_ysh = m_alg_xsh;
            }

            break;
        case 0x02:
            m_alg_jsh = m_alg_jch1;

            if ((m_via_orb & 0x01) == 0x00) {
                /* demultiplexor is on */
                m_alg_rsh = m_alg_xsh;
            }

            break;
        case 0x04:
            m_alg_jsh = m_alg_jch2;

            if ((m_via_orb & 0x01) == 0x00) {
                /* demultiplexor is on */

                if (m_alg_xsh > 0x80) {
                    m_alg_zsh = m_alg_xsh - 0x80;
                }
                else {
                    m_alg_zsh = 0;
                }
            }

            break;
        case 0x06:
            /* sound output line */
            m_alg_jsh = m_alg_jch3;
            break;
        }

        /* compare the current joystick direction with a reference */

        if (m_alg_jsh > m_alg_xsh) {
            m_alg_compare = 0x20;
        }
        else {
            m_alg_compare = 0;
        }

        /* compute the new "deltas" */

        m_alg_dx = (Ulong)m_alg_xsh - (Ulong)m_alg_rsh;
        m_alg_dy = (Ulong)m_alg_rsh - (Ulong)m_alg_ysh;
    }

    void IntUpdate() {
        // update IRQ and bit-7 of the ifr register after making an adjustment to ifr
        if (((m_via_ifr & 0x7f) != 0) & ((m_via_ier & 0x7f) != 0)) {
            m_via_ifr |= 0x80;
        }
        else {
            m_via_ifr &= 0x7f;
        }
    }

    public Uint8 Read8(Uint address) {
        Uint8 data = 0;

        if ((address & 0xe000) == 0xe000) {
            /* rom */

            data = m_rom[address & 0x1fff];
        }
        else if ((address & 0xe000) == 0xc000) {
            if ((address & 0x800) != 0) {
                /* ram */

                data = m_ram[address & 0x3ff];
            }
            else if ((address & 0x1000) != 0) {
                /* io */

                switch (address & 0xf) {
                case 0x0:
                    /* compare signal is an input so the value does not come from
                    * via_orb.
                    */
                    if ((m_via_acr & 0x80) != 0) {
                        /* timer 1 has control of bit 7 */

                        data = (Uint8) ((m_via_orb & 0x5f) | m_via_t1pb7 | m_alg_compare);
                    }
                    else {
                        /* bit 7 is being driven by via_orb */
                        data = (Uint8) ((m_via_orb & 0xdf) | m_alg_compare);
                    }

                    break;
                case 0x1:
                    /* register 1 also performs handshakes if necessary */
                    if ((m_via_pcr & 0x0e) == 0x08) {
                        /* if ca2 is in pulse mode or handshake mode, then it
                        * goes low whenever ira is read.
                        */

                        m_via_ca2 = 0;
                    }

                    /* cant fall through, so 0xf follows here too */
                    if ((m_via_orb & 0x18) == 0x08) {
                        /* the snd chip is driving port a */

                        data = (Uint8) m_soundRegisters[m_soundSelect];
                    }
                    else {
                        data = (Uint8) m_via_ora;
                    }
                    break;

                case 0xf:
                    if ((m_via_orb & 0x18) == 0x08) {
                        /* the snd chip is driving port a */

                        data = (Uint8) m_soundRegisters[m_soundSelect];
                    }
                    else {
                        data = (Uint8) m_via_ora;
                    }

                    break;
                case 0x2:
                    data = (Uint8) m_via_ddrb;
                    break;
                case 0x3:
                    data = (Uint8) m_via_ddra;
                    break;
                case 0x4:
                    /* T1 low order counter */
                    data = (Uint8) m_via_t1c;
                    m_via_ifr &= 0xbf; /* remove timer 1 interrupt flag */

                    m_via_t1on = 0; /* timer 1 is stopped */
                    m_via_t1int = 0;
                    m_via_t1pb7 = 0x80;

                    IntUpdate();

                    break;
                case 0x5:
                    /* T1 high order counter */
                    data = (Uint8) (m_via_t1c >> 8);

                    break;
                case 0x6:
                   /* T1 low order latch */
                    data = (Uint8) m_via_t1ll;
                    break;
                case 0x7:
                    /* T1 high order latch */
                    data = (Uint8) m_via_t1lh;
                    break;
                case 0x8:
                    /* T2 low order counter */
                    data = (Uint8) m_via_t2c;
                    m_via_ifr &= 0xdf; /* remove timer 2 interrupt flag */

                    m_via_t2on = 0; /* timer 2 is stopped */
                    m_via_t2int = 0;

                    IntUpdate ();

                    break;
                case 0x9:
                    /* T2 high order counter */
                    data = (Uint8) (m_via_t2c >> 8);
                    break;
                case 0xa:
                    data = (Uint8) m_via_sr;
                    m_via_ifr &= 0xfb; /* remove shift register interrupt flag */
                    m_via_srb = 0;
                    m_via_srclk = 1;

                    IntUpdate ();

                    break;
                case 0xb:
                    data = (Uint8) m_via_acr;
                    break;
                case 0xc:
                    data = (Uint8) m_via_pcr;
                    break;
                case 0xd:
                    /* interrupt flag register */

                    data = (Uint8) m_via_ifr;
                    break;
                case 0xe:
                    /* interrupt enable register */
                    data = (Uint8) (m_via_ier | 0x80);
                    break;
                }
            }
        }
        else if (address < 0x8000) {
            data = m_cartridge[address];
        }
        else {
            data = 0xff;
        }

        return data;
    }

    public void Write8(Uint address, Uint8 data) {
    if ((address & 0xe000) == 0xe000) {
        /* rom */
    }
    else if ((address & 0xe000) == 0xc000) {
        /* it is possible for both ram and io to be written at the same! */

        if ((address & 0x800) != 0) {
            m_ram[address & 0x3ff] = data;
        }

        if ((address & 0x1000) != 0) {
            switch (address & 0xf) {
            case 0x0:
                m_via_orb = data;

                SndUpdate ();

                AlgUpdate ();

                if ((m_via_pcr & 0xe0) == 0x80) {
                    /* if cb2 is in pulse mode or handshake mode, then it
                     * goes low whenever orb is written.
                     */

                    m_via_cb2h = 0;
                }

                break;
            case 0x1:
                /* register 1 also performs handshakes if necessary */

                if ((m_via_pcr & 0x0e) == 0x08) {
                    /* if ca2 is in pulse mode or handshake mode, then it
                     * goes low whenever ora is written.
                     */

                    m_via_ca2 = 0;
                }

                /* Cant fall through in C#, so also execute 0xf*/
                m_via_ora = data;

                SndUpdate ();

                /* output of port a feeds directly into the dac which then
                 * feeds the x axis sample and hold.
                 */

                m_alg_xsh = data ^ 0x80;

                AlgUpdate ();

                break;

            case 0xf:
                m_via_ora = data;

                SndUpdate ();

                /* output of port a feeds directly into the dac which then
                 * feeds the x axis sample and hold.
                 */

                m_alg_xsh = data ^ 0x80;

                AlgUpdate ();

                break;
            case 0x2:
                m_via_ddrb = data;
                break;
            case 0x3:
                m_via_ddra = data;
                break;
            case 0x4:
                /* T1 low order counter */

                m_via_t1ll = data;

                break;
            case 0x5:
                /* T1 high order counter */

                m_via_t1lh = data;
                m_via_t1c = (m_via_t1lh << 8) | m_via_t1ll;
                m_via_ifr &= 0xbf; /* remove timer 1 interrupt flag */

                m_via_t1on = 1; /* timer 1 starts running */
                m_via_t1int = 1;
                m_via_t1pb7 = 0;

                IntUpdate ();

                break;
            case 0x6:
                /* T1 low order latch */

                m_via_t1ll = data;
                break;
            case 0x7:
                /* T1 high order latch */

                m_via_t1lh = data;
                break;
            case 0x8:
                /* T2 low order latch */

                m_via_t2ll = data;
                break;
            case 0x9:
                /* T2 high order latch/counter */

                m_via_t2c = (data << 8) | m_via_t2ll;
                m_via_ifr &= 0xdf;

                m_via_t2on = 1; /* timer 2 starts running */
                m_via_t2int = 1;

                IntUpdate ();

                break;
            case 0xa:
                m_via_sr = data;
                m_via_ifr &= 0xfb; /* remove shift register interrupt flag */
                m_via_srb = 0;
                m_via_srclk = 1;

                IntUpdate ();

                break;
            case 0xb:
                m_via_acr = data;
                break;
            case 0xc:
                m_via_pcr = data;

                if ((m_via_pcr & 0x0e) == 0x0c) {
                    /* ca2 is outputting low */

                    m_via_ca2 = 0;
                }
                else {
                    /* ca2 is disabled or in pulse mode or is
                     * outputting high.
                     */

                    m_via_ca2 = 1;
                }

                if ((m_via_pcr & 0xe0) == 0xc0) {
                    /* cb2 is outputting low */

                    m_via_cb2h = 0;
                }
                else {
                    /* cb2 is disabled or is in pulse mode or is
                    * outputting high.
                    */

                    m_via_cb2h = 1;
                }

                break;
            case 0xd:
                /* interrupt flag register */

                m_via_ifr &= ~(data & 0x7f);
                IntUpdate ();

                break;
            case 0xe:
                /* interrupt enable register */

                if ((data & 0x80) != 0) {
                    m_via_ier |= data & 0x7f;
                }
                else {
                    m_via_ier &= ~(data & 0x7f);
                }

                IntUpdate ();

                break;
                }
            }
        }
        else if (address < 0x8000) {
            /* cartridge */
        }
    }

    void ViaSstep0() {
        // Perform a single cycle worth of via emulation via_sstep0 is the first postion of the emulation.
        Uint t2shift;

        if (m_via_t1on != 0) {
            m_via_t1c--;

            if ((m_via_t1c & 0xffff) == 0xffff) {
                /* counter just rolled over */

                if ((m_via_acr & 0x40) != 0) {
                    /* continuous interrupt mode */

                    m_via_ifr |= 0x40;
                    IntUpdate ();
                    m_via_t1pb7 = 0x80 - m_via_t1pb7;

                    /* reload counter */

                    m_via_t1c = (m_via_t1lh << 8) | m_via_t1ll;
                }
                else {
                    /* one shot mode */

                    if (m_via_t1int != 0) {
                        m_via_ifr |= 0x40;
                        IntUpdate ();
                        m_via_t1pb7 = 0x80;
                        m_via_t1int = 0;
                    }
                }
            }
        }

        if ((m_via_t2on != 0) && (m_via_acr & 0x20) == 0x00) {
            m_via_t2c--;

            if ((m_via_t2c & 0xffff) == 0xffff) {
                /* one shot mode */

                if (m_via_t2int != 0) {
                    m_via_ifr |= 0x20;
                    IntUpdate ();
                    m_via_t2int = 0;
                }
            }
        }

        /* shift counter */

        m_via_src--;

        if ((m_via_src & 0xff) == 0xff) {
            m_via_src = m_via_t2ll;

            if (m_via_srclk != 0) {
                t2shift = 1;
                m_via_srclk = 0;
            }
            else {
                t2shift = 0;
                m_via_srclk = 1;
            }
        }
        else {
            t2shift = 0;
        }

        if (m_via_srb < 8) {
            switch (m_via_acr & 0x1c) {
            case 0x00:
                /* disabled */
                break;
            case 0x04:
                /* shift in under control of t2 */

                if (t2shift != 0) {
                    /* shifting in 0s since cb2 is always an output */

                    m_via_sr <<= 1;
                    m_via_srb++;
                }

                break;
            case 0x08:
                /* shift in under system clk control */

                m_via_sr <<= 1;
                m_via_srb++;

                break;
            case 0x0c:
                /* shift in under cb1 control */
                break;
            case 0x10:
                /* shift out under t2 control (free run) */

                if (t2shift != 0) {
                    m_via_cb2s = (m_via_sr >> 7) & 1;

                    m_via_sr <<= 1;
                    m_via_sr |= m_via_cb2s;
                }

                break;
            case 0x14:
                /* shift out under t2 control */

                if (t2shift != 0) {
                    m_via_cb2s = (m_via_sr >> 7) & 1;

                    m_via_sr <<= 1;
                    m_via_sr |= m_via_cb2s;
                    m_via_srb++;
                }

                break;
            case 0x18:
                /* shift out under system clock control */

                m_via_cb2s = (m_via_sr >> 7) & 1;

                m_via_sr <<= 1;
                m_via_sr |= m_via_cb2s;
                m_via_srb++;

                break;
            case 0x1c:
                /* shift out under cb1 control */
                break;
            }

            if (m_via_srb == 8) {
                m_via_ifr |= 0x04;
                IntUpdate ();
            }
        }
    }

    void ViaSstep1() {
        // perform the second part of the via emulation
        if ((m_via_pcr & 0x0e) == 0x0a) {
            /* if ca2 is in pulse mode, then make sure
            * it gets restored to '1' after the pulse.
            */

            m_via_ca2 = 1;
        }

        if ((m_via_pcr & 0xe0) == 0xa0) {
            /* if cb2 is in pulse mode, then make sure
            * it gets restored to '1' after the pulse.
            */

            m_via_cb2h = 1;
        }
    }

    bool AlgAddToDrawList(Ulong index, long x0, long y0, long x1, long y1, Uint8 color) {
        if (index < 0 || index >= m_vector_draw_count) {
            return false;
        }

        Vector vector = (Vector)m_vectors_draw[index];

        if (x0 == vector.x0 && y0 == vector.y0 && x1 == vector.x1 && y1 == vector.y1) {
            vector.color = color;
            m_vectors_draw[index] = vector;

            return true;
        }

        return false;
    } 

    bool AlgAddToEraseList(Ulong index, long x0, long y0, long x1, long y1, Uint8 color) {    
        if (index < 0 || index >= m_vector_erase_count) {
            return false;
        }

        Vector vector = (Vector)m_vectors_erase[index];

        if (x0 == vector.x0 && y0 == vector.y0 && x1 == vector.x1 && y1 == vector.y1) {
            vector.color = color;
            m_vectors_erase[index] = vector;

            return true;
        }

        return false;
    } 

    void AlgAddline(long x0, long y0, long x1, long y1, Uint8 color) {
        Ulong key;
        Ulong index;

        key = (Ulong) x0;
        key = key * 31 + (Ulong) y0;
        key = key * 31 + (Ulong) x1;
        key = key * 31 + (Ulong) y1;
        key %= Vectrex.VECTOR_HASH;
        index = m_vector_hash[key];

        /* first check if the line to be drawn is in the current draw list.
        * if it is, then it is not added again.*/

        if (!AlgAddToDrawList(index, x0, y0, x1, y1, color)) {
            AlgAddToEraseList(index, x0, y0, x1, y1, color);

            Vector vector = new Vector(x0, y0, x1, y1, color);
            m_vectors_draw.Add(vector);

            m_vector_hash[key] = (Ulong)m_vector_draw_count;
            m_vector_draw_count++;
        }
    }

    void AlgSstep() {
        // perform a single cycle worth of analog emulation
        Ulong sig_dx, sig_dy;
        Uint sig_ramp;
        Uint sig_blank;

        if ((m_via_acr & 0x10) == 0x10) {
            sig_blank = m_via_cb2s;
        }
        else {
            sig_blank = m_via_cb2h;
        }

        if (m_via_ca2 == 0) {
            /* need to force the current point to the 'orgin' so just
            * calculate distance to origin and use that as dx,dy. */

            sig_dx = (Uint)(Vectrex.ALG_MAX_X / 2 - m_alg_curr_x);
            sig_dy = (Uint)(Vectrex.ALG_MAX_Y / 2 - m_alg_curr_y);
        }
        else {
            if ((m_via_acr & 0x80) != 0) {
                sig_ramp = m_via_t1pb7;
            }
            else {
                sig_ramp = m_via_orb & 0x80;
            }

            if (sig_ramp == 0) {
                sig_dx = (Uint)m_alg_dx;
                sig_dy = (Uint)m_alg_dy;
            }
            else {
                sig_dx = 0;
                sig_dy = 0;
            }
        }

        if (m_alg_vectoring == 0) {
            if (sig_blank == 1 &&
                m_alg_curr_x >= 0 && m_alg_curr_x < Vectrex.ALG_MAX_X &&
                m_alg_curr_y >= 0 && m_alg_curr_y < Vectrex.ALG_MAX_Y) {

                /* start a new vector */
                m_alg_vectoring = 1;
                m_alg_vector_x0 = m_alg_curr_x;
                m_alg_vector_y0 = m_alg_curr_y;
                m_alg_vector_x1 = m_alg_curr_x;
                m_alg_vector_y1 = m_alg_curr_y;
                m_alg_vector_dx = sig_dx;
                m_alg_vector_dy = sig_dy;
                m_alg_vector_color = (Uint8)m_alg_zsh;
            }
        }
        else {
            /* already drawing a vector ... check if we need to turn it off */

            if (sig_blank == 0) {
                /* blank just went on, vectoring turns off, and we've got a
                * new line.
                */

                m_alg_vectoring = 0;

                AlgAddline (m_alg_vector_x0, m_alg_vector_y0,
                            m_alg_vector_x1, m_alg_vector_y1,
                            m_alg_vector_color);
            }
            else if (sig_dx != m_alg_vector_dx ||
                    sig_dy != m_alg_vector_dy ||
                    (Uint8) m_alg_zsh != m_alg_vector_color) {

                /* the parameters of the vectoring processing has changed.
                * so end the current line.
                */

                AlgAddline (m_alg_vector_x0, m_alg_vector_y0,
                            m_alg_vector_x1, m_alg_vector_y1,
                            m_alg_vector_color);

                /* we continue vectoring with a new set of parameters if the
                * current point is not out of limits.
                */

                if (m_alg_curr_x >= 0 && m_alg_curr_x < Vectrex.ALG_MAX_X &&
                    m_alg_curr_y >= 0 && m_alg_curr_y < Vectrex.ALG_MAX_Y) {
                    m_alg_vector_x0 = m_alg_curr_x;
                    m_alg_vector_y0 = m_alg_curr_y;
                    m_alg_vector_x1 = m_alg_curr_x;
                    m_alg_vector_y1 = m_alg_curr_y;
                    m_alg_vector_dx = sig_dx;
                    m_alg_vector_dy = sig_dy;
                    m_alg_vector_color = (Uint8) m_alg_zsh;
                }
                else {
                    m_alg_vectoring = 0;
                }
            }
        }

        m_alg_curr_x += sig_dx;
        m_alg_curr_y += sig_dy;

        if (m_alg_vectoring == 1 &&
            m_alg_curr_x >= 0 && m_alg_curr_x < Vectrex.ALG_MAX_X &&
            m_alg_curr_y >= 0 && m_alg_curr_y < Vectrex.ALG_MAX_Y) {

            /* we're vectoring ... current point is still within limits so
            * extend the current vector. */

            m_alg_vector_x1 = m_alg_curr_x;
            m_alg_vector_y1 = m_alg_curr_y;
        }
    }
}