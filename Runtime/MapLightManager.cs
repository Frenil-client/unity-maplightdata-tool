using System.Collections.Generic;
using UnityEngine;

namespace MapLightDataTool
{
    /// <summary>
    /// 씬 조명 데이터를 중앙에서 관리하는 싱글톤 매니저.
    ///
    /// 핵심 설계:
    /// - Primary 슬롯: 현재 메인 씬의 기본 조명 (1개 유지)
    /// - Additive 스택: Additive 씬이 로드될수록 Push, 언로드 시 Pop 후 자동 복원
    /// - 적용 우선순위: Additive 스택 최상단 > Primary
    ///
    /// 진입점:
    /// - RegisterPrimary()  Primary 씬 조명 등록
    /// - PushAdditive()     Additive 씬 로드 시 스택 Push
    /// - PopAdditive()      Additive 씬 언로드 시 스택 Pop + 이전 조명 자동 복원
    /// - ApplyOverride()    런타임 중 현재 슬롯 교체 (낮/밤 전환 등)
    /// </summary>
    public class MapLightManager : MonoBehaviour
    {
        private static MapLightManager _instance;

        /// <summary>
        /// 싱글톤 인스턴스. 없으면 자동 생성합니다.
        /// </summary>
        public static MapLightManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[MapLightManager]");
                    _instance = go.AddComponent<MapLightManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // -----------------------------------------------------------------------

        /// <summary>현재 메인 씬의 기본 조명</summary>
        private MapLightData _primaryLight;

        /// <summary>Additive 씬 조명 스택. 최상단이 현재 적용 조명입니다.</summary>
        private readonly Stack<MapLightData> _additiveStack = new Stack<MapLightData>();

        // -----------------------------------------------------------------------

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // -----------------------------------------------------------------------

        /// <summary>
        /// Primary 씬 조명을 등록합니다.
        /// Additive 스택이 비어 있을 때만 즉시 RenderSettings에 반영됩니다.
        /// </summary>
        public void RegisterPrimary(MapLightData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[MapLightManager] RegisterPrimary: data가 null입니다.");
                return;
            }

            _primaryLight = data;

            if (_additiveStack.Count == 0)
                Apply(_primaryLight);
        }

        /// <summary>
        /// Additive 씬 조명을 스택에 Push하고 즉시 적용합니다.
        /// </summary>
        public void PushAdditive(MapLightData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[MapLightManager] PushAdditive: data가 null입니다.");
                return;
            }

            _additiveStack.Push(data);
            Apply(data);
        }

        /// <summary>
        /// Additive 씬 언로드 시 스택에서 해당 조명을 제거합니다.
        /// 제거 후 현재 최상단(또는 Primary)이 자동으로 복원됩니다.
        /// </summary>
        public void PopAdditive(MapLightData data)
        {
            if (_additiveStack.Count == 0)
                return;

            if (_additiveStack.Peek() == data)
            {
                // 최상단이 언로드 대상이면 바로 Pop
                _additiveStack.Pop();
            }
            else
            {
                // 중간 씬이 먼저 언로드되는 예외 케이스 - 스택 재구성
                RebuildStackWithout(data);
            }

            Apply(Current);
        }

        /// <summary>
        /// 런타임 중 현재 조명을 임시로 교체합니다. (낮/밤 전환 등)
        /// Additive 스택이 있으면 최상단을, 없으면 Primary 슬롯을 교체합니다.
        /// </summary>
        public void ApplyOverride(MapLightData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[MapLightManager] ApplyOverride: data가 null입니다.");
                return;
            }

            if (_additiveStack.Count > 0)
            {
                _additiveStack.Pop();
                _additiveStack.Push(data);
            }
            else
            {
                _primaryLight = data;
            }

            Apply(data);
        }

        // -----------------------------------------------------------------------

        /// <summary>현재 RenderSettings에 적용되어야 할 조명 (Additive 최상단 or Primary)</summary>
        public MapLightData Current =>
            _additiveStack.Count > 0 ? _additiveStack.Peek() : _primaryLight;

        /// <summary>data를 RenderSettings에 적용합니다.</summary>
        private void Apply(MapLightData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[MapLightManager] 적용할 조명 데이터가 없습니다.");
                return;
            }
            data.ApplyMapLightData();
        }

        /// <summary>
        /// 스택에서 특정 데이터를 제거하고 재구성합니다.
        /// Additive 씬이 중간 순서로 언로드되는 예외 케이스에서 호출됩니다.
        /// </summary>
        private void RebuildStackWithout(MapLightData target)
        {
            var temp = new List<MapLightData>(_additiveStack);
            temp.Reverse();
            _additiveStack.Clear();
            foreach (var item in temp)
            {
                if (item != target)
                    _additiveStack.Push(item);
            }
        }
    }
}
