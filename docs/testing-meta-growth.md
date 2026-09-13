# 메타 성장 검사

프로젝트 폴더에서 실행한다(.NET8 SDK, 외부 NuGet 패키지 없음).

```powershell
Set-Location 'C:\Users\82102\Documents\Codex\2026-09-12\x20\BackpackRTS'
dotnet run --project Tests/MetaFoundation/MetaFoundation.csproj -- .
python Tools/export_meta_economy.py --check
```

성공 표식은 `META_CHECKS_PASSED 82`, `APPROVED_ECONOMY_MATCHES_RUNTIME`다. 기반50개에 성장 검사32개를 추가했다.

- 승인 비용 차감, 경계에서 진화 요구, 최대 성장20종 확인.
- 공용 카드 종족 선택, 혼합 지불/다른 종족 지불 거절.
- 재료0장·스톤0개 상태의 진화 성공, 재료 부족 강화 거절.
- 실패 저장의 롤백, 오래된 revision 거절, 재실행·디스크 재접속 영수증 유지.
- H04의40회 강화+4회 진화로 정확히4,996장/164,180스톤/32종족스톤을 소비하고 Lv41 도달.
- 스탯 배율 분리, 선형 성장, 조회 객체 수정에 대한 상태 격리.

## Unity 직렬화 확인

Unity6000.4.0f1에서 프로젝트를 열고 **Prototype → Validate meta growth**를 실행한다. `META_UNITY_GROWTH_PASSED`가 성공 표식이다. N1→N4 강화 후 R 진화, 저장 재로딩, 동일 명령 재요청의 성장 영수증을 검증한다. 검사용 재료는 artifacts/meta-growth-unity 아래 별도 저장에만 지급한다.

게임 화면과 APK는 이번 PR에서 바뀌지 않는다. Unity 에디터 실행 검사는 준비되어 있으나 작업 환경에서 실행한 것으로 표시하지 않는다. .NET 검사는 Unity JsonUtility 대신 테스트용 JSON 어댑터를 사용한다.
