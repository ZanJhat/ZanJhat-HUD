using Engine;
using Game;
using System;
using ZanJhat.Core;

namespace ZanJhat.HUD
{
    public class TimeInfoHUDModule : HUDModule
    {
        public enum TimePhase
        {
            Dawn,
            Day,
            Dusk,
            Night
        }

        public SubsystemTimeOfDay m_subsystemTimeOfDay;
        public SubsystemSeasons m_subsystemSeasons;
        public SubsystemWeather m_subsystemWeather;

        public TimeInfoSettings m_timeInfoSettings;
        public float Scale => m_timeInfoSettings.Scale;
        public float m_lastScale;

        public AutoSizeCanvasWidget m_timeInfoWidget;
        public StackPanelWidget m_timeInfoPanel;

        public LabelWidget m_dayLabel;
        public LabelWidget m_timeLabel;
        public LabelWidget m_seasonLabel;
        public LabelWidget m_rainLabel;
        public LabelWidget m_fogLabel;

        public RectangleWidget m_seasonIcon;

        protected override void OnLoad()
        {
            m_subsystemTimeOfDay = Owner.Project.FindSubsystem<SubsystemTimeOfDay>(true);
            m_subsystemSeasons = Owner.Project.FindSubsystem<SubsystemSeasons>(true);
            m_subsystemWeather = Owner.Project.FindSubsystem<SubsystemWeather>(true);

            m_timeInfoSettings = HUDSettingsManager.TimeInfoSettings;
            m_lastScale = Scale;

            m_timeInfoWidget = ComponentGui.ControlsContainerWidget.Children.Find<AutoSizeCanvasWidget>("TimeInfoWidget", false);

            if (m_timeInfoWidget != null)
            {
                m_timeInfoPanel = m_timeInfoWidget.Children.Find<StackPanelWidget>("TimeInfoPanel");

                m_dayLabel = m_timeInfoPanel.Children.Find<StackPanelWidget>("Day")?.Children.Find<LabelWidget>(null);
                m_timeLabel = m_timeInfoPanel.Children.Find<StackPanelWidget>("Time")?.Children.Find<LabelWidget>(null);
                m_seasonLabel = m_timeInfoPanel.Children.Find<StackPanelWidget>("Season")?.Children.Find<LabelWidget>(null);
                m_rainLabel = m_timeInfoPanel.Children.Find<StackPanelWidget>("Rain")?.Children.Find<LabelWidget>(null);
                m_fogLabel = m_timeInfoPanel.Children.Find<StackPanelWidget>("Fog")?.Children.Find<LabelWidget>(null);

                m_seasonIcon = m_timeInfoPanel.Children.Find<StackPanelWidget>("Season")?.Children.Find<RectangleWidget>(null);
            }
            else
            {
                m_timeInfoWidget = new AutoSizeCanvasWidget
                {
                    Name = "TimeInfoWidget"
                };

                RectangleWidget background = new RectangleWidget
                {
                    FillColor = new Color(0, 0, 0, 128),
                    OutlineColor = Color.Transparent
                };
                m_timeInfoWidget.Children.Add(background);

                m_timeInfoPanel = new StackPanelWidget
                {
                    Name = "TimeInfoPanel",
                    Direction = LayoutDirection.Vertical
                };
                m_timeInfoWidget.Children.Add(m_timeInfoPanel);

                m_timeInfoPanel.Children.Add(CreateInfoRow("Day", "Textures/Time/Calendar", out _, out m_dayLabel));
                m_timeInfoPanel.Children.Add(CreateInfoRow("Time", "Textures/Time/Clock", out _, out m_timeLabel));
                m_timeInfoPanel.Children.Add(CreateInfoRow("Season", "Textures/Season/Summer", out m_seasonIcon, out m_seasonLabel));
                m_timeInfoPanel.Children.Add(CreateInfoRow("Rain", "Textures/Weather/Rain", out _, out m_rainLabel));
                m_timeInfoPanel.Children.Add(CreateInfoRow("Fog", "Textures/Weather/Fog", out _, out m_fogLabel));

                WidgetUtils.DisableHitTestRecursive(m_timeInfoWidget);
                ComponentGui.ControlsContainerWidget.Children.Insert(0, m_timeInfoWidget);
            }
        }

        public StackPanelWidget CreateInfoRow(string name, string iconPath, out RectangleWidget icon, out LabelWidget label)
        {
            StackPanelWidget row = new StackPanelWidget
            {
                Name = name,
                Direction = LayoutDirection.Horizontal,
                Margin = new Vector2(2f * Scale)
            };

            icon = new RectangleWidget
            {
                Size = new Vector2(24f * Scale),
                VerticalAlignment = WidgetAlignment.Center,
                FillColor = Color.White,
                OutlineColor = Color.Transparent,
                Subtexture = ContentManager.Get<Subtexture>(iconPath),
                Margin = new Vector2(2f * Scale)
            };
            row.Children.Add(icon);

            label = WidgetUtils.AddLabel(row, name, "", Color.White, 1f * Scale, false, new Vector2(2f * Scale), WidgetAlignment.Center, WidgetAlignment.Center);

            return row;
        }

        protected override void OnUnload()
        {
            if (m_timeInfoWidget != null && m_timeInfoWidget.ParentWidget != null)
            {
                m_timeInfoWidget.ParentWidget.Children.Remove(m_timeInfoWidget);
            }

            m_timeInfoWidget = null;
            m_timeInfoPanel = null;

            m_dayLabel = null;
            m_timeLabel = null;
            m_seasonLabel = null;
            m_rainLabel = null;
            m_fogLabel = null;

            m_seasonIcon = null;

            m_subsystemTimeOfDay = null;
            m_subsystemSeasons = null;
            m_subsystemWeather = null;
        }

        protected override void OnUpdate(float dt)
        {
            if (m_timeInfoWidget == null)
                return;

            if (!m_timeInfoSettings.Enable)
            {
                m_timeInfoWidget.IsVisible = false;
                return;
            }

            m_timeInfoWidget.IsVisible = true;

            if (Math.Abs(Scale - m_lastScale) > 0.001f)
            {
                ApplyScale();
                m_lastScale = Scale;
            }

            WidgetUtils.SetAnchor(m_timeInfoWidget, ComponentGui.ControlsContainerWidget, m_timeInfoSettings.Anchor, m_timeInfoSettings.MarginX, m_timeInfoSettings.MarginY);

            // Day
            m_dayLabel.Text = $"Day {GetDay()}";

            // Time
            GetTime(out int hour, out int minute);
            m_timeLabel.Text = $"{hour:00}:{minute:00}";

            // Season
            GetSeasonInfo(out Season season, out float seasonProgress);
            m_seasonLabel.Text = $"{season} {(int)(seasonProgress * 100f)}%";
            m_seasonIcon.Subtexture = ContentManager.Get<Subtexture>(GetSeasonIconPath(season));

            // Rain
            GetRainInfo(out bool raining, out float rainProgress, out float secondsRain);

            if (raining)
            {
                if (rainProgress < 0f)
                    m_rainLabel.Text = $"Raining (Infinity)";
                else
                    m_rainLabel.Text = $"Raining ({(int)(rainProgress * 100f)}%)";
            }
            else if (secondsRain > 0)
                m_rainLabel.Text = $"Rain in {TimeUtils.FormatTime(secondsRain, TimeUtils.TimeFormat.Short)}";
            else
                m_rainLabel.Text = "No rain";

            // Fog
            GetFogInfo(out bool fog, out float fogProgress, out float secondsFog);

            if (fog)
            {
                if (fogProgress < 0f)
                    m_fogLabel.Text = $"Foggy (Infinity)";
                else
                    m_fogLabel.Text = $"Foggy ({(int)(fogProgress * 100f)}%)";
            }
            else if (secondsFog > 0)
                m_fogLabel.Text = $"Fog in {TimeUtils.FormatTime(secondsFog, TimeUtils.TimeFormat.Short)}";
            else
                m_fogLabel.Text = "No fog";
        }

        public void ApplyScale()
        {
            foreach (Widget w in m_timeInfoPanel.Children)
            {
                if (w is StackPanelWidget row)
                {
                    row.Margin = new Vector2(2f * Scale);

                    foreach (Widget child in row.Children)
                    {
                        if (child is RectangleWidget icon)
                        {
                            icon.Size = new Vector2(24f * Scale);
                            icon.Margin = new Vector2(2f * Scale);
                        }
                        else if (child is LabelWidget label)
                        {
                            label.FontScale = 1f * Scale;
                            label.Margin = new Vector2(2f * Scale);
                        }
                    }
                }
            }
        }

        public string GetSeasonIconPath(Season season)
        {
            switch (season)
            {
                case Season.Summer:
                    return "Textures/Season/Summer";
                case Season.Autumn:
                    return "Textures/Season/Autumn";
                case Season.Winter:
                    return "Textures/Season/Winter";
                case Season.Spring:
                    return "Textures/Season/Spring";
                default:
                    return "Textures/Gui/Unavailable";
            }
        }

        public int GetDay()
        {
            return (int)Math.Floor(m_subsystemTimeOfDay.Day);
        }

        public void GetTime(out int hour, out int minute)
        {
            float totalHours = m_subsystemTimeOfDay.TimeOfDay * 24f;

            hour = (int)totalHours;
            minute = (int)((totalHours - hour) * 60f);
        }

        public TimePhase GetTimePhase()
        {
            float t = m_subsystemTimeOfDay.TimeOfDay;

            if (IntervalUtils.IsBetween(t, m_subsystemTimeOfDay.DawnStart, m_subsystemTimeOfDay.DayStart))
                return TimePhase.Dawn;

            if (IntervalUtils.IsBetween(t, m_subsystemTimeOfDay.DayStart, m_subsystemTimeOfDay.DuskStart))
                return TimePhase.Day;

            if (IntervalUtils.IsBetween(t, m_subsystemTimeOfDay.DuskStart, m_subsystemTimeOfDay.NightStart))
                return TimePhase.Dusk;

            return TimePhase.Night;
        }

        public void GetSeasonInfo(out Season season, out float progress)
        {
            season = m_subsystemSeasons.Season;
            progress = m_subsystemSeasons.TimeOfSeason;
        }

        public void GetRainInfo(out bool isRaining, out float progress, out float secondsUntilRain)
        {
            double time = m_subsystemWeather.m_subsystemGameInfo.TotalElapsedGameTime;

            if (m_subsystemWeather.IsPrecipitationStarted)
            {
                isRaining = true;
                secondsUntilRain = 0f;

                if (double.IsPositiveInfinity(m_subsystemWeather.m_precipitationEndTime))
                {
                    progress = -1f; // Dấu hiệu nhận biết mưa vô cực
                }
                else
                {
                    double duration = m_subsystemWeather.m_precipitationEndTime - m_subsystemWeather.m_precipitationStartTime;
                    double p = (time - m_subsystemWeather.m_precipitationStartTime) / duration;

                    progress = MathUtils.Saturate((float)p);
                }
            }
            else
            {
                isRaining = false;
                progress = 0f;

                if (time < m_subsystemWeather.m_precipitationStartTime)
                {
                    secondsUntilRain = (float)(m_subsystemWeather.m_precipitationStartTime - time);
                }
                else
                {
                    secondsUntilRain = 0f;
                }
            }
        }

        public void GetFogInfo(out bool isFog, out float progress, out float secondsUntilFog)
        {
            double time = m_subsystemWeather.m_subsystemGameInfo.TotalElapsedGameTime;

            if (m_subsystemWeather.IsFogStarted)
            {
                isFog = true;
                secondsUntilFog = 0f;

                if (double.IsPositiveInfinity(m_subsystemWeather.m_fogEndTime))
                {
                    progress = -1f; // Dấu hiệu nhận biết sương mù vô cực
                }
                else
                {
                    progress = MathUtils.Saturate(m_subsystemWeather.FogProgress);
                }
            }
            else
            {
                isFog = false;
                progress = 0f;

                if (time < m_subsystemWeather.m_fogStartTime)
                {
                    secondsUntilFog = (float)(m_subsystemWeather.m_fogStartTime - time);
                }
                else
                {
                    secondsUntilFog = 0f;
                }
            }
        }
    }
}
