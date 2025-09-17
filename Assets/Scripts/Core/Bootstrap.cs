using System.Collections.Generic;
using IdleFramework.Core;
using IdleFramework.Data;
using IdleFramework.DI;
using IdleFramework.Economy;
using IdleFramework.Platform;
using IdleFramework.Save;
using IdleFramework.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IdleFramework.Core
{
    /// <summary>
    /// Bootstraps core services and creates a minimal runtime UI when none exists.
    /// </summary>
    public sealed class Bootstrap : MonoBehaviour
    {
        [SerializeField]
        private float tickRate = 10f;

        private TickManager _tickManager = null!;
        private EconomyService _economy = null!;
        private PrestigeService _prestige = null!;
        private SaveService _saveService = null!;
        private ContentDB _content = null!;
        private readonly Dictionary<string, Text> _currencyLabels = new Dictionary<string, Text>();
        private readonly List<GeneratorView> _generatorViews = new List<GeneratorView>();
        private readonly List<UpgradeView> _upgradeViews = new List<UpgradeView>();
        private Text _prestigeLabel = null!;
        private Button _prestigeButton = null!;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            var locator = new ServiceLocator();
            ServiceLocator.SetLocator(locator);

            var skinName = PlayerPrefs.GetString("skinName", "Classic");
            _content = new ContentDB();
            _content.LoadSkin(skinName);

            var timeProvider = new SystemTimeProvider();
            locator.Register<ITimeProvider>(timeProvider);

            _tickManager = gameObject.AddComponent<TickManager>();
            _tickManager.SetTickRate(tickRate);

            _economy = new EconomyService(_content);
            _prestige = new PrestigeService(_economy, _content, timeProvider);
            _saveService = new SaveService(_content, _economy, _prestige, timeProvider);

            locator.Register(_tickManager);
            locator.Register(_economy);
            locator.Register(_prestige);
            locator.Register(_saveService);
            locator.Register(_content);

            locator.Register(CreatePlatformServices());

            _tickManager.OnTick += HandleTick;
            _economy.OnStateChanged += RefreshUi;
        }

        private void Start()
        {
            _saveService.Load();
            EnsureRuntimeUi();
            RefreshUi();
        }

        private void OnDestroy()
        {
            if (_tickManager != null)
            {
                _tickManager.OnTick -= HandleTick;
            }

            if (_economy != null)
            {
                _economy.OnStateChanged -= RefreshUi;
            }
        }

        private void HandleTick(float delta)
        {
            _economy.Tick(delta);
            _saveService.Update(delta);
        }

        private void EnsureRuntimeUi()
        {
            if (FindObjectOfType<Canvas>() != null)
            {
                return;
            }

            var canvasGo = new GameObject("RuntimeCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            var root = new GameObject("HUD");
            root.transform.SetParent(canvas.transform, false);
            var rootLayout = root.AddComponent<VerticalLayoutGroup>();
            rootLayout.spacing = 12;
            rootLayout.padding = new RectOffset(12, 12, 12, 12);

            BuildCurrenciesPanel(root.transform);
            BuildGeneratorsPanel(root.transform);
            BuildUpgradesPanel(root.transform);
            BuildPrestigePanel(root.transform);
        }

        private void BuildCurrenciesPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "Currencies");
            foreach (var currency in _content.Currencies.Values)
            {
                var text = CreateText(panel.transform, $"{currency.Name}: 0");
                _currencyLabels[currency.Id] = text;
            }
        }

        private void BuildGeneratorsPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "Generators");
            foreach (var generator in _content.Generators.Values)
            {
                var card = new GameObject(generator.Name);
                card.transform.SetParent(panel.transform, false);
                var layout = card.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 8;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = false;

                var nameText = CreateText(card.transform, generator.Name);
                nameText.alignment = TextAnchor.MiddleLeft;

                var levelText = CreateText(card.transform, "Lv 0");
                levelText.alignment = TextAnchor.MiddleLeft;

                var costText = CreateText(card.transform, "Cost: 0");
                costText.alignment = TextAnchor.MiddleLeft;

                var buttonObj = new GameObject("BuyButton");
                buttonObj.transform.SetParent(card.transform, false);
                var image = buttonObj.AddComponent<Image>();
                image.color = new Color(0.2f, 0.6f, 0.2f, 1f);
                var button = buttonObj.AddComponent<Button>();
                var layoutElement = buttonObj.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = 90;
                layoutElement.preferredHeight = 36;
                var buttonText = CreateText(buttonObj.transform, "Buy");
                buttonText.alignment = TextAnchor.MiddleCenter;

                var id = generator.Id;
                button.onClick.AddListener(() =>
                {
                    if (_economy.TryBuyGeneratorLevel(id, out _))
                    {
                        RefreshUi();
                    }
                });

                _generatorViews.Add(new GeneratorView
                {
                    Id = generator.Id,
                    Level = levelText,
                    Cost = costText,
                    Button = button
                });
            }
        }

        private void BuildUpgradesPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "Upgrades");
            foreach (var upgrade in _content.Upgrades.Values)
            {
                var row = new GameObject(upgrade.Name);
                row.transform.SetParent(panel.transform, false);
                var layout = row.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 8;

                var label = CreateText(row.transform, upgrade.Name);
                label.alignment = TextAnchor.MiddleLeft;

                var buttonObj = new GameObject("BuyButton");
                buttonObj.transform.SetParent(row.transform, false);
                var image = buttonObj.AddComponent<Image>();
                image.color = new Color(0.2f, 0.4f, 0.6f, 1f);
                var button = buttonObj.AddComponent<Button>();
                var layoutElement = buttonObj.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = 90;
                layoutElement.preferredHeight = 36;
                var buttonText = CreateText(buttonObj.transform, "Buy");
                buttonText.alignment = TextAnchor.MiddleCenter;

                var id = upgrade.Id;
                button.onClick.AddListener(() =>
                {
                    if (_economy.TryBuyUpgrade(id, out _))
                    {
                        RefreshUi();
                    }
                });

                _upgradeViews.Add(new UpgradeView
                {
                    Id = upgrade.Id,
                    Label = label,
                    Button = button
                });
            }
        }

        private void BuildPrestigePanel(Transform parent)
        {
            var panel = CreatePanel(parent, "Prestige");
            _prestigeLabel = CreateText(panel.transform, "Prestige: 0");

            var buttonObj = new GameObject("PrestigeButton");
            buttonObj.transform.SetParent(panel.transform, false);
            var image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.6f, 0.2f, 0.2f, 1f);
            _prestigeButton = buttonObj.AddComponent<Button>();
            var layout = buttonObj.AddComponent<LayoutElement>();
            layout.preferredWidth = 120;
            layout.preferredHeight = 40;
            var text = CreateText(buttonObj.transform, "Prestige");
            text.alignment = TextAnchor.MiddleCenter;

            _prestigeButton.onClick.AddListener(() =>
            {
                var earned = _prestige.PerformPrestige();
                if (earned > 0)
                {
                    _saveService.Save();
                    RefreshUi();
                }
            });
        }

        private GameObject CreatePanel(Transform parent, string title)
        {
            var panel = new GameObject(title + "Panel");
            panel.transform.SetParent(parent, false);
            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6;
            CreateText(panel.transform, title).fontStyle = FontStyle.Bold;
            return panel;
        }

        private Text CreateText(Transform parent, string text)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var uiText = go.AddComponent<Text>();
            uiText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            uiText.text = text;
            uiText.color = Color.white;
            uiText.fontSize = 18;
            return uiText;
        }

        private void RefreshUi()
        {
            UpdateCurrencies();
            UpdateGenerators();
            UpdateUpgrades();
            UpdatePrestige();
        }

        private void UpdateCurrencies()
        {
            foreach (var kvp in _currencyLabels)
            {
                var balance = _economy.Balances.TryGetValue(kvp.Key, out var value) ? value : 0;
                kvp.Value.text = $"{_content.Currencies[kvp.Key].Name}: {NumberFormatter.Format(balance)}";
            }
        }

        private void UpdateGenerators()
        {
            foreach (var view in _generatorViews)
            {
                var state = _economy.Generators[view.Id];
                view.Level.text = $"Lv {state.Level}";
                view.Cost.text = $"Cost: {NumberFormatter.Format(_economy.GetNextGeneratorCost(view.Id))}";
                view.Button.interactable = _economy.CanBuyGeneratorLevel(view.Id, out _);
            }
        }

        private void UpdateUpgrades()
        {
            foreach (var view in _upgradeViews)
            {
                var upgrade = _content.Upgrades[view.Id];
                var purchased = _economy.PurchasedUpgrades.Contains(view.Id);
                view.Label.text = $"{upgrade.Name} ({NumberFormatter.Format(upgrade.Price)})" + (purchased ? " [Owned]" : string.Empty);
                view.Button.interactable = !purchased && _economy.CanPurchaseUpgrade(view.Id, out _);
            }
        }

        private void UpdatePrestige()
        {
            var preview = _prestige.PreviewPrestige();
            _prestigeLabel.text = $"Prestige: {_prestige.PrestigeCurrency} (+{preview})";
            _prestigeButton.interactable = preview > 0;
        }

        private IPlatformServices CreatePlatformServices()
        {
#if UNITY_STANDALONE
            return new Platform.SteamServicesStub();
#elif UNITY_ANDROID || UNITY_IOS
            return new Platform.MobileServicesStub();
#else
            return new PlatformServicesNull();
#endif
        }

        private sealed class PlatformServicesNull : IPlatformServices
        {
            public void UnlockAchievement(string id) { }
            public void ReportStat(string id, double value) { }
            public void ShowAchievements() { }
            public void RequestCloudSaveSync() { }
            public void ShowStorePage(string productId) { }
            public void ShowAd(string placementId) { }
        }

        private sealed class GeneratorView
        {
            public string Id = string.Empty;
            public Text Level = null!;
            public Text Cost = null!;
            public Button Button = null!;
        }

        private sealed class UpgradeView
        {
            public string Id = string.Empty;
            public Text Label = null!;
            public Button Button = null!;
        }
    }
}
