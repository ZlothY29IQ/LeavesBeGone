using System;
using System.Collections;
using System.IO;
using System.Reflection;
using BepInEx;
using UnityEngine;

namespace GorillaTimeChanger
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        private bool inModdedRoom;

        private bool _isRaining;
        private bool _isOpen;

        private bool wasRightPrimaryPressed;
        private bool wasLeftPrimaryPressed;

        private Texture2D dayTexture, eveningTexture, nightTexture, morningTexture;
        private Texture2D rainTexture, clearTexture;

        private GameObject dayObj, eveningObj, nightObj, morningObj;
        private GameObject rainObj, clearObj;
        private GameObject canvasObj;

        private const int DayTime = 3, EveningTime = 7, NightTime = 0, MorningTime = 1;

        private bool isRaining
        {
            get { return _isRaining; }
            set
            {
                if (_isRaining != value)
                {
                    _isRaining = value;

                    if (BetterDayNightManager.instance.weatherCycle != null)
                    {
                        for (int i = 0; i < BetterDayNightManager.instance.weatherCycle.Length; i++)
                            BetterDayNightManager.instance.weatherCycle[i] = value
                                ? BetterDayNightManager.WeatherType.Raining
                                : BetterDayNightManager.WeatherType.None;
                    }

                    rainObj.SetActive(value);
                    clearObj.SetActive(!value);
                }
            }
        }

        private bool isOpen
        {
            get { return _isOpen; }
            set
            {
                if (_isOpen != value)
                {
                    _isOpen = value;

                    if (value)
                    {
                        canvasObj.transform.position = GorillaTagger.Instance.mainCamera.transform.position +
                                                       GorillaTagger.Instance.mainCamera.transform.forward * 0.5f;
                        canvasObj.transform.rotation =
                            Quaternion.LookRotation(GorillaTagger.Instance.mainCamera.transform.forward);
                        StartCoroutine(GrowCoroutine(canvasObj));
                    }
                    else
                    {
                        StartCoroutine(ShrinkCoroutine(canvasObj));
                    }
                }
            }
        }

        private void Start() => GorillaTagger.OnPlayerSpawned(OnGameInitialized);

        private void OnGameInitialized()
        {
            NetworkSystem.Instance.OnJoinedRoomEvent += OnRoomJoin;
            NetworkSystem.Instance.OnReturnedToSinglePlayer += OnRoomLeave;

            dayTexture = LoadEmbeddedTexture2D("GorillaTimeChanger.Resources.Day.png");
            eveningTexture = LoadEmbeddedTexture2D("GorillaTimeChanger.Resources.Evening.png");
            nightTexture = LoadEmbeddedTexture2D("GorillaTimeChanger.Resources.Night.png");
            morningTexture = LoadEmbeddedTexture2D("GorillaTimeChanger.Resources.Morning.png");
            rainTexture = LoadEmbeddedTexture2D("GorillaTimeChanger.Resources.Rain.png");
            clearTexture = LoadEmbeddedTexture2D("GorillaTimeChanger.Resources.Clear.png");

            dayObj = LoadTexture2DIntoWorldSpace(dayTexture, "Day");
            eveningObj = LoadTexture2DIntoWorldSpace(eveningTexture, "Evening");
            nightObj = LoadTexture2DIntoWorldSpace(nightTexture, "Night");
            morningObj = LoadTexture2DIntoWorldSpace(morningTexture, "Morning");
            rainObj = LoadTexture2DIntoWorldSpace(rainTexture, "Rain");
            clearObj = LoadTexture2DIntoWorldSpace(clearTexture, "Clear");

            canvasObj = new GameObject("GorillaTimeChangerCanvas");
            canvasObj.transform.localScale = Vector3.zero;

            dayObj.transform.SetParent(canvasObj.transform);
            eveningObj.transform.SetParent(canvasObj.transform);
            nightObj.transform.SetParent(canvasObj.transform);
            morningObj.transform.SetParent(canvasObj.transform);
            rainObj.transform.SetParent(canvasObj.transform);
            clearObj.transform.SetParent(canvasObj.transform);

            dayObj.transform.localScale = Vector3.one * 0.1f;
            eveningObj.transform.localScale = Vector3.one * 0.1f;
            nightObj.transform.localScale = Vector3.one * 0.1f;
            morningObj.transform.localScale = Vector3.one * 0.1f;
            rainObj.transform.localScale = Vector3.one * 0.1f;
            clearObj.transform.localScale = Vector3.one * 0.1f;

            dayObj.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            eveningObj.transform.localPosition = new Vector3(0.1f, 0f, 0f);
            nightObj.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            morningObj.transform.localPosition = new Vector3(-0.1f, 0f, 0f);
            rainObj.transform.localPosition = new Vector3(0f, -0.25f, 0f);
            clearObj.transform.localPosition = new Vector3(0f, -0.25f, 0f);

            dayObj.AddComponent<PressableButton>().OnPress = () => BetterDayNightManager.instance.SetTimeOfDay(DayTime);
            eveningObj.AddComponent<PressableButton>().OnPress =
                () => BetterDayNightManager.instance.SetTimeOfDay(EveningTime);
            nightObj.AddComponent<PressableButton>().OnPress =
                () => BetterDayNightManager.instance.SetTimeOfDay(NightTime);
            morningObj.AddComponent<PressableButton>().OnPress =
                () => BetterDayNightManager.instance.SetTimeOfDay(MorningTime);
            
            GameObject rainButton = new GameObject("RainButton");
            rainButton.transform.SetParent(canvasObj.transform);
            rainButton.transform.localScale = Vector3.one * 0.1f;
            rainButton.transform.localPosition = new Vector3(0f, -0.25f, 0f);
            rainButton.AddComponent<PressableButton>().OnPress = () => isRaining = !isRaining;
        }

        private Texture2D LoadEmbeddedTexture2D(string path)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(path))
            {
                if (stream == null) return null;

                byte[] imageData = new byte[stream.Length];
                stream.Read(imageData, 0, imageData.Length);

                Texture2D texture2d = new Texture2D(2, 2);
                texture2d.LoadImage(imageData);

                return texture2d;
            }
        }

        private GameObject LoadTexture2DIntoWorldSpace(Texture2D texture, string name)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(obj.GetComponent<Collider>());
            obj.name = name;

            obj.GetComponent<Renderer>().material = new Material(Shader.Find("UI/Default"));
            obj.GetComponent<Renderer>().material.mainTexture = texture;

            return obj;
        }

        private void Update()
        {
            if (!inModdedRoom) return;

            if (ControllerInputPoller.instance.leftControllerPrimaryButton &&
                ControllerInputPoller.instance.rightControllerPrimaryButton && !wasLeftPrimaryPressed &&
                !wasRightPrimaryPressed)
            {
                wasLeftPrimaryPressed = true;
                wasRightPrimaryPressed = true;
                isOpen = !isOpen;
            }
            else if (!ControllerInputPoller.instance.leftControllerPrimaryButton &&
                     !ControllerInputPoller.instance.rightControllerPrimaryButton)
            {
                wasLeftPrimaryPressed = false;
                wasRightPrimaryPressed = false;
            }
        }

        private IEnumerator ShrinkCoroutine(GameObject obj)
        {
            float duration = 0.1f;
            float elapsed = 0f;

            Vector3 initialScale = obj.transform.localScale;
            Vector3 targetScale = Vector3.zero;

            while (elapsed < duration)
            {
                obj.transform.localScale = Vector3.Lerp(initialScale, targetScale, elapsed / duration);
                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            obj.transform.localScale = targetScale;
            obj.SetActive(false);
        }

        private IEnumerator GrowCoroutine(GameObject obj)
        {
            obj.SetActive(true);

            float duration = 0.1f;
            float elapsed = 0f;

            Vector3 initialScale = obj.transform.localScale;
            Vector3 targetScale = Vector3.one;

            while (elapsed < duration)
            {
                obj.transform.localScale = Vector3.Lerp(initialScale, targetScale, elapsed / duration);
                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            obj.transform.localScale = targetScale;
        }

        private void OnRoomJoin()
        {
            inModdedRoom = NetworkSystem.Instance.GameModeString.Contains("MODDED");

            if (inModdedRoom)
                isRaining = false;
        }

        private void OnRoomLeave()
        {
            inModdedRoom = false;
            isOpen = false;
            BetterDayNightManager.instance.currentSetting = TimeSettings.Normal;
        }
    }

    public class PressableButton : MonoBehaviour
    {
        public Action OnPress;

        private bool isPressing;
        private static float lastTime;

        // Hacky solution for no colliders (not even sure if quads support OnTriggerEnter)
        private void Update()
        {
            float rightDistance = Vector3.Distance(GorillaTagger.Instance.rightHandTriggerCollider.transform.position,
                transform.position);
            float leftDistance = Vector3.Distance(GorillaTagger.Instance.leftHandTriggerCollider.transform.position,
                transform.position);

            if (rightDistance < 0.1f || leftDistance < 0.1f)
            {
                if (!isPressing && Time.time - lastTime > 0.2f)
                {
                    isPressing = true;
                    lastTime = Time.time;
                    GorillaTagger.Instance.StartVibration(leftDistance < rightDistance, 0.3f, 0.05f);
                    OnPress?.Invoke();
                }
            }
            else
            {
                isPressing = false;
            }
        }
    }
}