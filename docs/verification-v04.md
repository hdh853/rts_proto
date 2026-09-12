# v0.4 검증 기록

- 전체 C# 소스: Unity 참조 어셈블리를 사용하는 Roslyn 컴파일 통과. 기존 미사용 notice 필드 경고 1개.
- 시뮬레이션 독립 검사: 66/66 통과. 이전 51개 + 신규 15개. Unity 저장 검사 3개도 통과하여 실제 Editor 검사 총 69/69 통과.
- 던전 기존 정지 사례: 같은 필드의 적을 대상으로 전진하는 회귀 검사 통과.
- 영웅 기존 정지 건물 배치: 15초 후 전진 회귀 검사 통과. 별도 60개 배치 진단에서도 정지 미검출.
- PowerShell 빌드 도우미: 구문 검사 통과.
- Unity Windows/Android v0.4 빌드: 로그의 Build Finished Success, WINDOWS_BUILD_OK, ANDROID_BUILD_OK 확인. 각 빌드에서 PROTOTYPE_CHECKS_PASSED 69 확인.
- APK: outputs/BackpackRTS-0.4.0.apk, 35,828,232 bytes.
- 자동 화면 15장 생성 확인. 휴먼/오크 외형 비교, 합성 후보 반투명, 유닛 정보, 타워 사거리, 마법 원형, 배치 미리보기, 던전 화면을 직접 확인.
- 터치·실기기 프레임·실제 플레이 감각: 미검증.
- 화면 보완 사항: 전장 유닛이 작아 세부 무기 차이가 약하고, 건물별 실루엣 차이도 추가 아트 작업 필요. Clash Royale 수준의 최종 아트 품질로 판정하지 않음. 일부 전장 상단 안내 문구가 잘리고, 티어 비교 화면의 열 제목 정렬과 넓은 건물 간격도 보완 필요.
- PR: 구현 검토와 실기기 피드백 단계. 병합 승인 전.

브랜치: feature/v04-visuals-navigation. v0.3 브랜치를 바탕으로 작성. 마지막 원격 확인 시 v0.3이 main에 미병합 상태였으므로 PR 생성 직전 기준 브랜치를 다시 확인한다. 미병합이면 feature/placement-decks-projectiles를 기준으로 비교한다. 자동 병합하지 않는다.
