# v0.3 검증 기록 · 2026-09-12

- 독립 실행 전투/입력 상태 검사51개 통과.
- Unity 6000.4.0f1 실제 검사54개 통과(저장/보상 검증 포함).
- Windows 개발 빌드: WINDOWS_BUILD_OK, 종료코드0.
- Windows 자동 실행: 메인, 덱, 초록/빨강 미리보기, 일반 전장, 던전, 합성 화면 저장. 게임 로그에서 예외 확인되지 않음.
- Android 개발 빌드: ANDROID_BUILD_OK, 종료코드0, APK 35,750,700바이트.
- Android manifest: com.hdh853.rtsproto, versionName0.3.0, minSdk26, targetSdk35, arm64-v8a.
- 실제 기기 터치·발열·최대 병력 성능은 미검증.

사용자가 빌드 중 메모리 참조 오류 창을 몇 차례 보았다고 보고했다. Unity·게임 로그에 해당 접근 위반 기록이 없고, 해당 시간대 Windows Application의1000/1001/1002/1026 오류 이벤트도 조회되지 않았다. 발생 프로그램과 원인은 미확정이다. 빌드 성공을 근거로 해당 보고가 해결되었다고 판단하지 않는다. 사용자는 오류가 GitHub 쪽이었다고 추가 확인했다. 같은 시간 Git 인증 도구에서 Windows wincredman 저장 실패가 확인되어 관련 가능성이 있으나 정확한 실행 파일과 접근 위반 원인까지 특정하지는 못했다.

변경은 feature/placement-decks-projectiles 브랜치에 보관하며 초기 v0.2 기준과 분리한다. PR은 별도 원격 생성 후 URL을 기록한다.
