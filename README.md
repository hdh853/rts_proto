# Backpack RTS Prototype

Unity 6000.4.0f1 기반 세로형 RTS + 배낭 합성 프로토타입. Android 우선이며 Windows 빌드로도 검증한다.

현재 v0.3: 종족 생산6종과 공용 기능·마법8종에서 8장 편성. 중복 없는 4장 손패, 반투명 배치 미리보기와 손을 뗄 때 결제, 1칸 금광·일꾼6명, T4 합성, 넓어진 전장·건물 사이 경로, 지연 피해 투사체, 고정 적 던전·영웅·영구 성장을 포함한다.

- [최신 변경 명세 v0.3](docs/specs/003-placement-decks-projectiles.md)
- [v0.3 테스트 안내](docs/testing-v03.md)

- [현재 명세 v0.2](docs/specs/001-prototype.md)
- [유닛별 60개 티어 데이터](docs/specs/002-balance-data.md)
- [실행 및 수동 검증 안내](docs/testing.md)
- [SDD와 PR 작업 방식](docs/decisions/001-sdd-workflow.md)

Unity Hub에서 이 폴더를 추가하고 `Assets/Scenes/Boot.unity`를 연다. Game 뷰는 9:16. `Prototype/Validate rules`로 핵심 검사, `Prototype/Build Windows` 또는 `Prototype/Build Android`로 개발 빌드를 만든다. Android에는 동일 버전의 Android Build Support와 SDK·NDK·OpenJDK가 필요하다.

현재 Windows·Android 빌드와 Unity 규칙 검사54개는 성공했다. 메인·덱·초록/빨강 배치·전장·던전·T4 화면을 확인했다. 실기기 터치·성능 검증은 남아 있다. 초기 수치는 튜닝 가능한 가설이며, 종족 밸런스가 검증 완료된 것은 아니다. 아트는 직접 생성한 임시 카툰 모형이다.

초기 프로토타입 후 모든 변경은 명세 → 사용자 승인 → feature 브랜치 → 검증 → PR 순서로 진행한다. 사용자 승인 없이 PR을 병합하지 않는다.
