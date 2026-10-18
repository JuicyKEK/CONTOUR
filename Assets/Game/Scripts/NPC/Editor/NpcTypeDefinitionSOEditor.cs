using Game.Scripts.NPC.Runtime.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.NPC.Editor
{
    /// <summary>
    /// Инспектор типа бота: настройки по разделам с русскими подписями; разделы, которые к выбранному режиму
    /// не относятся, скрыты (шкала злости - только в режиме "Шкала злости" и т.п.). Подсказки - из [Tooltip] полей.
    /// Тот же инспектор показывается внутри инспектора NpcController (см. NpcControllerEditor).
    /// </summary>
    [CustomEditor(typeof(NpcTypeDefinitionSO))]
    public class NpcTypeDefinitionSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var mode = (NpcAggressionMode)serializedObject.FindProperty("m_AggressionMode").intValue;
            var attackKind = (NpcAttackKind)serializedObject.FindProperty("m_AttackKind").intValue;

            if (Section("Общее", "general"))
            {
                Field("m_TypeName", "Название типа");
                Field("m_AnimationSoundProfile", "Анимации и звуки (профиль)");
            }

            if (Section("Обзор", "view"))
            {
                Field("m_ViewDistance", "Дальность обзора, м");
                var halfAngle = Field("m_ViewHalfAngle", "Половина угла обзора, °");
                Hint($"Конус обзора целиком - {Mathf.Clamp(halfAngle.floatValue, 0f, 180f) * 2f:0}°.");
                var lineOfSight = Field("m_RequireLineOfSight", "Стены загораживают обзор");

                if (lineOfSight.boolValue)
                {
                    Field("m_LineOfSightObstacles", "Что загораживает (слои)");
                }
                else
                {
                    Hint("Видит игрока сквозь стены (в пределах дальности и угла).");
                }

                Field("m_EyeHeight", "Высота глаз, м");
            }

            if (Section("Агрессия", "aggression"))
            {
                Field("m_AggressionMode", "Откуда агрессия");

                if (mode == NpcAggressionMode.Fixed || mode == NpcAggressionMode.Manual)
                {
                    Field("m_StartsAggressive", mode == NpcAggressionMode.Fixed ? "Агрессивен" : "Агрессивен с самого начала");
                }

                if (mode == NpcAggressionMode.InfectionZoneLinked)
                {
                    Field("m_InfectionAggressionThreshold", "Агрессивен с уровня заражения зоны");
                }

                if (mode == NpcAggressionMode.AngerMeter)
                {
                    var rise = Field("m_AngerRisePerSecond", "Рост злости за секунду на виду (шкала 0..1)");
                    Hint(rise.floatValue > 0f
                        ? $"На виду пустая шкала заполнится примерно за {1f / rise.floatValue:0.#} с (при заражении 0%)."
                        : "Сама шкала не растёт - только от 'неправильных' действий игрока.");
                    Field("m_AngerDecayPerSecond", "Спад злости за секунду, когда игрока не видно");
                    Field("m_AngerInfectionMultiplier", "Во сколько раз быстрее злится при заражении 100%");
                    Field("m_InstantAggressionInfectionLevel", "Сразу агрессивен при виде игрока с заражения");
                    Field("m_AngerActions", "Действия игрока (сигналы сюжета)");
                    Field("m_AngerAfterCalm", "Злость после возврата к роли");
                    Hint("Злость можно менять и из UnityEvent: NpcController.AddAnger(число).");
                }
            }

            if (Section("Скорости, м/с", "speeds"))
            {
                var patrol = Field("m_PatrolSpeed", "Спокойно (обход, роль, команды)");
                var search = Field("m_SearchSpeed", "Поиск игрока");
                var chase = Field("m_ChaseSpeed", "Погоня");
                Field("m_PanicSpeed", "Паника");

                if (search.floatValue <= patrol.floatValue || search.floatValue >= chase.floatValue)
                {
                    EditorGUILayout.HelpBox("Скорость поиска должна быть больше спокойной и меньше погони.", MessageType.Warning);
                }
            }

            if (Section("Атака", "attack"))
            {
                Field("m_CatchDistance", "Дистанция атаки, м");
                Field("m_AttackKind", "Атака");
                Field("m_AttackHitDelay", "Замах до удара, с");
                Field("m_AttackDuration", "Длительность атаки, с");
                Field("m_AttackCooldown", "Пауза перед следующей атакой, с");

                if (attackKind == NpcAttackKind.Hit)
                {
                    Field("m_AfterHit", "После удара");
                }
            }

            if (Section("Погоня", "chase"))
            {
                Field("m_LoseSightGiveUpDelay", "Бежит к месту, где видел игрока, не дольше, с");
                Field("m_ChaseStopsOnlyByStory", "Сюжетная погоня (всегда знает, где игрок)");
            }

            if (Section("Поиск игрока", "search"))
            {
                Field("m_SearchRadius", "Радиус поиска вокруг, м");
                Field("m_LocalSearchDuration", "Сколько ищет вокруг, с");
                Field("m_SearchLookAroundMin", "Осматривается на точке от, с");
                Field("m_SearchLookAroundMax", "Осматривается на точке до, с");
                var zoneLevel = Field("m_ZoneSearchInfectionLevel", "Ищет по всей зоне с заражения");

                if (zoneLevel.floatValue <= 100f)
                {
                    Field("m_ZoneSearchDuration", "Сколько ищет по всей зоне, с (0 - пока не найдёт)");
                    Field("m_ZoneSearchFallbackRadius", "Радиус 'всей зоны', если зона не найдена, м");
                }
                else
                {
                    Hint("По всей зоне не ищет никогда - после поиска вокруг возвращается к спокойному поведению.");
                }
            }

            if (Section("Двери", "doors"))
            {
                var canOpen = Field("m_CanOpenDoors", "Открывает незапертые двери");
                Field("m_KnockInterval", "Пауза между попытками открыть запертую дверь, с");
                Field("m_LockedDoorGiveUpDelay", "В погоне дёргает запертую дверь, с");
                Field("m_LockedDoorTriesWhenCalm", "Вне погони: попыток, потом ищет обход");
                Field("m_DoorDetectionMask", "Слои дверей");
                Field("m_DoorDetectionDistance", "Замечает дверь впереди за, м");
                Field("m_DoorDetectionRadius", "Радиус поиска двери, м");

                if (canOpen.boolValue)
                {
                    Field("m_PatrolDoorOpenerKind", "Открывает спокойно как");
                    Field("m_ChaseDoorOpenerKind", "Открывает в погоне как");
                    Hint("Если у бота есть облики (NpcAppearance) - как открывать двери, задаёт облик.");
                }
            }

            if (Section("Блуждание (Wander) и сдача у двери", "wander"))
            {
                Field("m_WanderRadius", "Радиус блуждания, м");
                Field("m_WanderMinInterval", "Новая точка не чаще, с");
                Field("m_WanderMaxInterval", "Новая точка не реже, с");
                Field("m_WanderChaseSuppressDuration", "Сдавшись у запертой двери, не возвращается в погоню, с");
            }

            serializedObject.ApplyModifiedProperties();
        }

        private SerializedProperty Field(string name, string label)
        {
            var property = serializedObject.FindProperty(name);

            if (property == null)
            {
                EditorGUILayout.HelpBox($"Нет поля {name}", MessageType.Error);
                return null;
            }

            EditorGUILayout.PropertyField(property, new GUIContent(label, property.tooltip), true);
            return property;
        }

        private static void Hint(string text)
        {
            EditorGUILayout.LabelField(text, EditorStyles.wordWrappedMiniLabel);
        }

        private static bool Section(string title, string key)
        {
            string stateKey = "NpcTypeDefinitionSOEditor." + key;
            bool isOpen = SessionState.GetBool(stateKey, true);
            EditorGUILayout.Space(4f);
            bool newOpen = EditorGUILayout.BeginFoldoutHeaderGroup(isOpen, title);
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (newOpen != isOpen)
            {
                SessionState.SetBool(stateKey, newOpen);
            }

            return newOpen;
        }
    }
}
