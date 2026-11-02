using System.Runtime.InteropServices;
using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;

[BepInPlugin("com.user.firewatchfly", "Firewatch Fly", "1.0.0")]
public class FlyMod : BaseUnityPlugin
{
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private const int VK_K = 0x4B;
    private const int VK_F4 = 0x73;
    private const int VK_F6 = 0x75;

    // Die Mod startet normal. K, F4 oder F6 schaltet Fliegen an/aus.
    private bool flyMode = false;
    private bool prevKeyDown = false;
    private float lastToggleTime = -10f;
    private float nextFindTime = 0f;
    private float nextHeartbeat = 0f;
    private int heartbeatCount = 0;
    private bool loggedFirstUpdate = false;
    private bool hasFocus = true;
    private CharacterController player;
    private Transform moveTarget;
    private Transform extraTarget;
    private Camera moveCam;
    private System.Collections.Generic.List<Renderer> hidden = new System.Collections.Generic.List<Renderer>();
    private bool hiddenInit = false;
    private Behaviour moveScript;
    private bool moveScriptWasOn = false;

    private bool dipping = false;
    private float dipTime = 0f;
    private float dipDepth = 0f;
    private Transform dipCam;
    private Vector3 dipBase;

    // true = Koerper beim Fliegen verstecken, false = Koerper immer sichtbar
    private const bool HideBodyWhileFlying = false;

    // Koerper-Verhalten beim Fliegen/Fallen
    private const float TurnSpeed = 540f;        // Grad pro Sekunde, mit der sich der Koerper zur Blickrichtung dreht
    private const float MaxLeanForward = 10f;    // leichte Neigung nach vorne/hinten
    private const float MaxLeanSide = 7f;        // leichte Neigung zur Seite
    private const float BobAmount = 0.015f;      // leichtes Atmen/Wippen in Metern

    private Transform bodyRoot;
    private Vector3 bodyBasePos;
    private Quaternion bodyBaseRot;
    private Behaviour animBridge;
    private bool animBridgeWasOn = false;
    private Animator bodyAnim;
    private AnimatorControllerParameter[] animParams;
    private float animSpeed = 0f;

    private float bodyTurn = 0f;
    private float leanX = 0f;
    private float leanZ = 0f;

    // Animations-Steuerung greift tief in Henrys Skripte ein (kann z.B. das Funkgeraet beeinflussen).
    // Deshalb standardmaessig AUS.
    private const bool EnableAnimationControl = false;

    private const float DipDuration = 0.45f;

    private bool falling = false;
    private float fallVel = 0f;
    private float fallTime = 0f;

    private const float Gravity = 18f;
    private const float MaxFallSpeed = 35f;
    private const float BaseSpeed = 6f;
    private const float FastMultiplier = 3f;

    void Awake()
    {
        gameObject.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(gameObject);
        Logger.LogInfo("Firewatch Fly 1.0.0 geladen. Start: Fliegen AUS. K / F4 / F6 = Fliegen an/aus.");
    }

    void OnDestroy()
    {
        if (player != null)
        {
            player.enabled = true;
            RestoreAfterFly();
        }
        Logger.LogInfo("Firewatch Fly wurde zerstoert (OnDestroy).");
    }

    void OnApplicationFocus(bool focus)
    {
        hasFocus = focus;
    }

    bool WindowsKeyDown()
    {
        return (GetAsyncKeyState(VK_K) & 0x8000) != 0
            || (GetAsyncKeyState(VK_F4) & 0x8000) != 0
            || (GetAsyncKeyState(VK_F6) & 0x8000) != 0;
    }

    void Update()
    {
        if (!loggedFirstUpdate)
        {
            loggedFirstUpdate = true;
            Logger.LogInfo("Update laeuft.");
        }

        // Herzschlag: alle 5 Sekunden, maximal 12 Mal
        if (heartbeatCount < 12 && Time.unscaledTime >= nextHeartbeat)
        {
            nextHeartbeat = Time.unscaledTime + 5f;
            heartbeatCount++;
            Logger.LogInfo("Herzschlag, Szene: " + SceneManager.GetActiveScene().name + ", Fokus: " + hasFocus);
        }

        bool unityPressed = Input.GetKeyDown(KeyCode.F4) || Input.GetKeyDown(KeyCode.F6) || Input.GetKeyDown(KeyCode.K);

        bool winDown = hasFocus && WindowsKeyDown();
        bool winPressed = winDown && !prevKeyDown;
        prevKeyDown = winDown;

        if ((unityPressed || winPressed) && Time.unscaledTime - lastToggleTime > 0.3f)
        {
            lastToggleTime = Time.unscaledTime;
            Logger.LogInfo("Taste gedrueckt (Unity: " + unityPressed + ", Windows: " + winPressed + ")");
            SetFly(!flyMode);
        }
    }

    void LateUpdate()
    {
        UpdateDip();

        if (!flyMode && !falling) return;

        FindPlayer();
        if (player == null && falling) { falling = false; return; }
        if (player == null) return;

        // Beim Fliegen den CharacterController abschalten (kein Boden, keine Kollision)
        if (player.enabled) player.enabled = false;

        if (!hiddenInit)
        {
            hiddenInit = true;
            FreezeAndHide();
        }
        for (int i = 0; i < hidden.Count; i++)
        {
            if (hidden[i] != null && hidden[i].enabled) hidden[i].enabled = false;
        }

        Camera cam = Camera.main;
        if (cam == null) return;

        if (falling)
        {
            StepFall(cam);
            if (falling) AnimateBody(cam, Vector3.zero);
            return;
        }

        Vector3 forward = cam.transform.forward;
        forward.y = 0f;
        forward.Normalize();
        Vector3 right = cam.transform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 dir = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) dir += forward;
        if (Input.GetKey(KeyCode.S)) dir -= forward;
        if (Input.GetKey(KeyCode.D)) dir += right;
        if (Input.GetKey(KeyCode.A)) dir -= right;
        if (Input.GetKey(KeyCode.Space)) dir += Vector3.up;
        if (Input.GetKey(KeyCode.LeftControl)) dir -= Vector3.up;

        float speed = BaseSpeed;
        if (Input.GetKey(KeyCode.LeftShift)) speed *= FastMultiplier;

        if (moveTarget == null || moveCam != cam) ComputeMoveTarget(cam);

        Vector3 delta = dir * speed * Time.deltaTime;
        moveTarget.position += delta;
        if (extraTarget != null) extraTarget.position += delta;

        AnimateBody(cam, dir);
    }

    // Sucht das kleinste Objekt ueber dem CharacterController, das auch die Kamera enthaelt,
    // damit Koerper und Kamera zusammen bewegt werden.
    void ComputeMoveTarget(Camera cam)
    {
        moveCam = cam;
        moveTarget = null;
        extraTarget = null;

        Transform t = player.transform;
        while (t != null)
        {
            if (cam.transform.IsChildOf(t)) { moveTarget = t; break; }
            t = t.parent;
        }

        Logger.LogInfo("Spieler-Pfad: " + PathOf(player.transform));
        string comps = "";
        foreach (Component c in player.GetComponents<Component>())
        {
            if (c != null) comps += c.GetType().Name + ", ";
        }
        Logger.LogInfo("Komponenten am Spieler: " + comps);
        Logger.LogInfo("Kamera-Pfad: " + PathOf(cam.transform));

        if (moveTarget == null)
        {
            moveTarget = player.transform;
            extraTarget = cam.transform.root;
            Logger.LogInfo("Kamera liegt NICHT unter dem Spieler. Bewege beide getrennt.");
        }
        else
        {
            Logger.LogInfo("Bewege gemeinsames Objekt: " + moveTarget.name);
        }
    }

    // Friert Henrys Bewegungs-Skript ein und versteckt sein Koerpermodell waehrend des Fliegens.
    void FreezeAndHide()
    {
        moveScript = player.GetComponent("vgCoolGuyMoveController") as Behaviour;
        moveScriptWasOn = false;
        if (moveScript != null && moveScript.enabled)
        {
            moveScript.enabled = false;
            moveScriptWasOn = true;
        }
        Logger.LogInfo("Bewegungs-Skript gefunden: " + (moveScript != null) + ", eingefroren: " + moveScriptWasOn);

        // Die Animations-Bruecke des Spiels abschalten, damit die Mod die Animation selbst steuern kann
        animBridge = null;
        animBridgeWasOn = false;
        bodyAnim = null;
        animParams = null;
        animSpeed = 0f;
        string plist = "";
        if (EnableAnimationControl)
        {
            animBridge = player.GetComponent("vgPlayerAnimationBridge") as Behaviour;
            if (animBridge != null && animBridge.enabled)
            {
                animBridge.enabled = false;
                animBridgeWasOn = true;
            }
        }
        // Animator nur zum Auslesen der Parameter-Namen (fuers Log), ohne etwas zu veraendern
        Animator logAnim = player.GetComponentInChildren<Animator>();
        if (logAnim != null)
        {
            foreach (AnimatorControllerParameter ap in logAnim.parameters) plist += ap.name + ":" + ap.type + ", ";
            if (EnableAnimationControl)
            {
                bodyAnim = logAnim;
                animParams = logAnim.parameters;
            }
        }
        Logger.LogInfo("Animationssteuerung: " + (EnableAnimationControl ? "AN" : "AUS") + ", Animator gefunden: " + (logAnim != null));
        Logger.LogInfo("Animator-Parameter: " + plist);

        hidden.Clear();
        string names = "";
        int visibleCount = 0;
        foreach (Renderer r in player.GetComponentsInChildren<Renderer>())
        {
            if (r.enabled)
            {
                visibleCount++;
                names += r.name + ", ";
                if (HideBodyWhileFlying) hidden.Add(r);
            }
        }
        Logger.LogInfo("Koerperteile: " + visibleCount + " (" + names + "), versteckt: " + hidden.Count);

        // Koerper-Wurzel suchen (direktes Kind von PlayerModel mit dem Modell)
        bodyRoot = null;
        SkinnedMeshRenderer smr = player.GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr != null)
        {
            Transform t = smr.transform;
            while (t.parent != null && t.parent != player.transform) t = t.parent;
            if (t.parent == player.transform) bodyRoot = t;
        }
        if (bodyRoot != null)
        {
            bodyBasePos = bodyRoot.localPosition;
            bodyBaseRot = bodyRoot.localRotation;
        }
        leanX = 0f;
        leanZ = 0f;
        bodyTurn = 0f;
        Logger.LogInfo("Koerper-Wurzel: " + (bodyRoot != null ? bodyRoot.name : "nicht gefunden"));
    }

    void RestoreAfterFly()
    {
        if (bodyRoot != null)
        {
            bodyRoot.localPosition = bodyBasePos;
            bodyRoot.localRotation = bodyBaseRot;
        }
        bodyRoot = null;

        for (int i = 0; i < hidden.Count; i++)
        {
            if (hidden[i] != null) hidden[i].enabled = true;
        }
        hidden.Clear();
        if (animBridge != null && animBridgeWasOn) animBridge.enabled = true;
        animBridge = null;
        animBridgeWasOn = false;
        bodyAnim = null;
        animParams = null;
        if (moveScript != null && moveScriptWasOn) moveScript.enabled = true;
        Logger.LogInfo("Spiel-Skripte wieder aktiviert.");
        moveScript = null;
        moveScriptWasOn = false;
    }

    string PathOf(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    void FindPlayer()
    {
        if (player != null) return;
        if (Time.unscaledTime < nextFindTime) return;
        nextFindTime = Time.unscaledTime + 1f;

        CharacterController[] all = (CharacterController[])Object.FindObjectsOfType(typeof(CharacterController));
        Logger.LogInfo("Suche Spieler: " + all.Length + " CharacterController gefunden.");
        if (all.Length > 0)
        {
            player = all[0];
            foreach (CharacterController c in all)
            {
                if (c.GetComponent("vgPlayerController") != null) { player = c; break; }
            }
            Logger.LogInfo("Spieler gefunden: " + player.gameObject.name);
        }
    }

    void SetFly(bool on)
    {
        flyMode = on;
        if (on)
        {
            bool wasFalling = falling;
            falling = false;
            moveTarget = null;
            extraTarget = null;
            // Wenn schon eingefroren (Fall laeuft), nicht erneut einfrieren,
            // sonst gehen die gemerkten Zustaende verloren
            if (!wasFalling) hiddenInit = false;
        }
        else
        {
            FindPlayer();
            if (player != null && !player.enabled)
            {
                // Fliegen beendet: die Mod laesst Henry jetzt selbst mit Schwerkraft fallen
                falling = true;
                fallVel = 0f;
                fallTime = 0f;

                // Waehrend des Falls ist der Koerper wieder sichtbar
                for (int i = 0; i < hidden.Count; i++)
                {
                    if (hidden[i] != null) hidden[i].enabled = true;
                }
                hidden.Clear();
            }
        }
        Logger.LogInfo("Fly Mode: " + (on ? "AN" : "AUS"));
    }

    // Ein Schritt des freien Falls: beschleunigt nach unten, bis der Boden erreicht ist.
    void StepFall(Camera cam)
    {
        if (moveTarget == null || moveCam != cam) ComputeMoveTarget(cam);

        fallTime += Time.deltaTime;
        fallVel += Gravity * Time.deltaTime;
        if (fallVel > MaxFallSpeed) fallVel = MaxFallSpeed;
        float step = fallVel * Time.deltaTime;

        Vector3 sc = player.transform.lossyScale;
        float scale = Mathf.Max(sc.x, Mathf.Max(sc.y, sc.z));
        float radius = player.radius * scale;
        float height = Mathf.Max(player.height * scale, radius * 2f);
        float bottomOffset = height / 2f;
        Vector3 center = player.transform.TransformPoint(player.center);

        float nearest = float.MaxValue;
        RaycastHit[] hits = Physics.RaycastAll(center, Vector3.down, bottomOffset + step + 0.05f, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit h in hits)
        {
            if (h.collider.transform.IsChildOf(moveTarget)) continue;
            if (h.distance < nearest) nearest = h.distance;
        }

        float move = step;
        bool landed = false;
        if (nearest < float.MaxValue)
        {
            float dist = nearest - bottomOffset;
            if (dist <= step + 0.02f)
            {
                move = Mathf.Max(dist - 0.02f, 0f);
                landed = true;
            }
        }

        moveTarget.position += Vector3.down * move;
        if (extraTarget != null) extraTarget.position += Vector3.down * move;

        if (landed || fallTime > 20f)
        {
            Logger.LogInfo("Gelandet nach " + fallTime.ToString("F2") + " Sekunden, Fallgeschwindigkeit " + fallVel.ToString("F1"));
            FinishFall();
        }
    }

    // Steuert die Koerper-Animation: Gehen, wenn du dich bewegst, sonst Stehen.
    void ApplyAnimation(Vector3 moveDir)
    {
        if (bodyAnim == null || animParams == null) return;

        moveDir.y = 0f;
        float target = Mathf.Clamp01(moveDir.magnitude);
        animSpeed = Mathf.Lerp(animSpeed, target, Mathf.Clamp01(8f * Time.deltaTime));

        foreach (AnimatorControllerParameter ap in animParams)
        {
            string n = ap.name.ToLower();
            if (ap.type == AnimatorControllerParameterType.Float)
            {
                bool speedLike = n.Contains("speed") || n.Contains("vel") || n.Contains("move")
                              || n.Contains("walk") || n.Contains("run") || n.Contains("forward");
                if (speedLike && !n.Contains("vert")) bodyAnim.SetFloat(ap.nameHash, animSpeed);
            }
            else if (ap.type == AnimatorControllerParameterType.Bool)
            {
                if (n.Contains("ground")) bodyAnim.SetBool(ap.nameHash, !falling);
                else if (n.Contains("air") || n.Contains("fall") || n.Contains("jump")) bodyAnim.SetBool(ap.nameHash, falling);
            }
        }
    }

    // Dreht nur das sichtbare Koerpermodell (nicht Henry selbst!) in Blickrichtung
    // und gibt ihm leichte, natuerliche Bewegung. Die Kamera wird dadurch nicht beeinflusst.
    void AnimateBody(Camera cam, Vector3 moveDir)
    {
        if (player == null) return;

        ApplyAnimation(moveDir);

        if (bodyRoot == null) return;

        Transform pt = player.transform;
        float playerYaw = pt.eulerAngles.y;

        // Wie weit muss sich das Modell gegenueber Henry drehen, um zur Kamera zu zeigen?
        float turnTarget = Mathf.DeltaAngle(playerYaw, cam.transform.eulerAngles.y);
        bodyTurn = Mathf.MoveTowardsAngle(bodyTurn, turnTarget, TurnSpeed * Time.deltaTime);

        // Leichte Neigung in Bewegungsrichtung (in der Blickrichtung des Koerpers berechnet)
        float faceYaw = playerYaw + bodyTurn;
        Quaternion faceQ = Quaternion.Euler(0f, faceYaw, 0f);

        moveDir.y = 0f;
        Vector3 local = Quaternion.Inverse(faceQ) * moveDir;
        float targetX = Mathf.Clamp(local.z, -1f, 1f) * MaxLeanForward;
        float targetZ = Mathf.Clamp(-local.x, -1f, 1f) * MaxLeanSide;
        float k = Mathf.Clamp01(6f * Time.deltaTime);
        leanX = Mathf.Lerp(leanX, targetX, k);
        leanZ = Mathf.Lerp(leanZ, targetZ, k);

        Quaternion yawQ = Quaternion.AngleAxis(bodyTurn, Vector3.up);
        Quaternion leanQ = Quaternion.AngleAxis(leanX, faceQ * Vector3.right)
                         * Quaternion.AngleAxis(leanZ, faceQ * Vector3.forward);
        Quaternion baseWorld = pt.rotation * bodyBaseRot;

        bodyRoot.rotation = leanQ * yawQ * baseWorld;

        float bob = Mathf.Sin(Time.time * 2.2f) * BobAmount;
        bodyRoot.localPosition = bodyBasePos + Vector3.up * bob;
    }

    // Abfedern: Die Kamera geht beim Aufkommen kurz nach unten und federt zurueck.
    void StartDip()
    {
        if (fallVel < 3f) return;
        Camera c = Camera.main;
        if (c == null) return;

        // Falls noch ein altes Abfedern laeuft, zuerst zuruecksetzen
        if (dipping && dipCam != null) dipCam.localPosition = dipBase;

        dipCam = c.transform;
        dipBase = dipCam.localPosition;
        dipDepth = Mathf.Clamp(fallVel / MaxFallSpeed * 0.12f, 0.02f, 0.12f);
        dipTime = 0f;
        dipping = true;
    }

    void UpdateDip()
    {
        if (!dipping) return;
        if (dipCam == null) { dipping = false; return; }

        dipTime += Time.deltaTime;
        float t = dipTime / DipDuration;
        if (t >= 1f)
        {
            dipCam.localPosition = dipBase;
            dipping = false;
            return;
        }

        float a = t < 0.25f ? t / 0.25f : 1f - (t - 0.25f) / 0.75f;
        a = Mathf.SmoothStep(0f, 1f, a);
        dipCam.localPosition = dipBase + Vector3.down * dipDepth * a;
    }

    void FinishFall()
    {
        falling = false;
        StartDip();
        if (player != null)
        {
            ResolveOverlap();
            player.enabled = true;
            RestoreAfterFly();
        }
        moveTarget = null;
        extraTarget = null;
        hiddenInit = false;
    }

    // Setzt den Spieler nach oben, bis er in nichts mehr feststeckt, bevor die Kollision wieder an geht.
    void ResolveOverlap()
    {
        Camera cam = Camera.main;
        if (cam != null && (moveTarget == null || moveCam != cam)) ComputeMoveTarget(cam);
        Transform target = moveTarget != null ? moveTarget : player.transform;

        Vector3 s = player.transform.lossyScale;
        float scale = Mathf.Max(s.x, Mathf.Max(s.y, s.z));
        float radius = player.radius * scale;
        float height = Mathf.Max(player.height * scale, radius * 2f);
        Vector3 center = player.transform.TransformPoint(player.center);

        int steps = 0;
        for (int i = 0; i < 80; i++)
        {
            Vector3 p1 = center + Vector3.up * (height / 2f - radius);
            Vector3 p2 = center - Vector3.up * (height / 2f - radius);
            Collider[] hits = Physics.OverlapCapsule(p1, p2, radius * 0.95f, ~0, QueryTriggerInteraction.Ignore);

            bool blocked = false;
            foreach (Collider c in hits)
            {
                if (c.transform.IsChildOf(target)) continue;
                blocked = true;
                break;
            }
            if (!blocked) break;

            target.position += Vector3.up * 0.25f;
            if (extraTarget != null) extraTarget.position += Vector3.up * 0.25f;
            center += Vector3.up * 0.25f;
            steps++;
        }
        Logger.LogInfo("Landung: " + steps + " Schritte nach oben gesetzt.");
    }
}
