using System.Collections.Generic;
using UnityEngine;

namespace MapLightDataTool
{
    /// <summary>
    /// 씬 조명 데이터를 중앙에서 관리하는 싱글톤 매니저.
    ///
    /// 구조:
    /// - Primary 조명: 현재 메인 씬의 기본 조명 (1개 유지)
    /// - Additive 스택: Additive 씬이 쌓일수록 Push, 언로드 시 Pop
    /// - 최상단 스택 조명이 항상 RenderSettings에 적용됨
    ///
    /// 적용 우선순위: Additive 스택 최상단 > Primary
    /// </summary>
    public class MapLightManager : MonoBehaviour
    {
        private static MapLightManager _instance;

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

        private MapLightData _primaryLight;
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
        /// Primary 씬 조명을 등록하고 적용합니다.
        /// Additive 스택이 비어 있을 때만 즉시 반영됩니다.
        /// </summary>
        public void RegisterPrimary(MapLightData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[MapLightManager] RegisterPrimary: data가 null입니다.");
                return;
            }

            _primaryLight = data;

            // Additive 씬이 없을 때만 Primary를 바로 적용
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
        /// Additive 씬 언로드 시 스택에서 해당 조명을 제거하고
        /// 이전 조명(스택 최상단 or Primary)으로 자동 복원합니다.
        /// </summary>
        public void PopAdditive(MapLightData data)
        {
            if (_additiveStack.Count == 0)
                return;

            // 스택 최상단이 언로드 대상이면 바로 Pop
            if (_additiveStack.Peek() == data)
            {
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
        /// Primary이면 _primaryLight를, Additive이면 스택 최상단을 교체합니다.
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
                // 스택 최상단 교체
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

        /// <summary>현재 적용되어야 할 조명 (Additive 최상단 or Primary)</summary>
        public MapLightData Current =>
            _additiveStack.Count > 0 ? _additiveStack.Peek() : _primaryLight;

        private void Apply(MapLightData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[MapLightManager] 적용할 조명 데이터가 없습니다.");
                return;
            }
            data.ApplyMapLightData();
        }

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
