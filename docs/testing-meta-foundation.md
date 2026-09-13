# 메타게임 기반 검사

## 자동 검사

PowerShell에서 프로젝트 폴더로 이동해 실행한다. .NET8 SDK가 필요하며 외부 NuGet 패키지는 사용하지 않는다. 검사 저장은 artifacts/meta-checks의 고유 폴더에 생성하며 실제 사용자 저장은 수정하지 않는다.

```powershell
Set-Location 'C:\Users\82102\Documents\Codex\2026-09-12\x20\BackpackRTS'
dotnet run --project Tests/MetaFoundation/MetaFoundation.csproj -- .
python Tools/export_meta_economy.py --check
```

성공 표식은 `META_CHECKS_PASSED`와 `APPROVED_ECONOMY_MATCHES_RUNTIME`다. Python이 PATH에 없으면 설치된 Python 실행 파일의 전체 경로를 사용한다.

검사 범위: 비용 합계/성장 항목, 기본 지급, 저장 이전/백업/재실행, 스테이지 잠금, 단일 전투, 보상 예약, 만석 동의, 보상 동결, 개봉 경계시각, 완료 미수령, 저장 실패, 재요청/다른 내용 재요청, 디스크 재로딩, 영웅/카드 변환, 1,000회 보상 총량, 저장 경합/손상 처리.

## Unity 실제 직렬화 검사

Unity Hub에서 BackpackRTS 프로젝트를 Unity6000.4.0f1로 연다. 컴파일 완료 후 상단 메뉴 **Prototype → Validate meta foundation**을 선택한다. Console에 `META_UNITY_SERIALIZATION_PASSED`가 나와야 한다. artifacts/meta-unity의 임시 저장을 사용하며 실제 progress.json을 건드리지 않는다.

이 검사는 Unity JsonUtility와 파일 어댑터가 새 필드·상자·재요청 기록을 유지하는지 확인한다. .NET 검사의 JSON 어댑터와 다르므로 별도로 수행해야 한다. 현재 게임 UI는 바뀌지 않으므로 이 PR만으로 APK를 다시 빌드할 필요는 없다.

## 이번 작업 결과

- 승인 원본 ↔ Unity 리소스 JSON 일치 확인.
- .NET 도메인 자동 검사 통과(최종 개수는 검증 로그 참조).
- 기존 전투 검사88개 통과. 구 ProgressStore의 Unity 전용 저장 검사3개는 이 독립 실행에 포함하지 않는다.
- Unity6000.4.0f1 어셈블리를 참조한 전체 C# 컴파일 성공. 기존 PrototypeGame.notice 미사용 경고1개 유지.
- Unity 에디터 실행 직렬화 검사와 Windows/Android 빌드는 이 작업에서 실행하지 않았다.

새 UI, 실제 승리 정산 연동, 성장 전투 적용과 서버 보안은 이번 검사로 검증된 것으로 취급하지 않는다.
