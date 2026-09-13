# v0.5 검증 기록

- Unity 6000.4.0f1: Windows 및 Android 빌드 성공. 각 로그의 PROTOTYPE_CHECKS_PASSED 91, WINDOWS_BUILD_OK, ANDROID_BUILD_OK 확인.
- APK: outputs/BackpackRTS-0.5.0.apk, 35,835,132 bytes.
- 자동 PNG 16장 생성. drag-merge-preview.png의 청록색 복제 모형과 후보 강조, merge.png의 T4 선택 버블, placement-valid.png의 07:58 표시 확인.
- 플레이어 로그: Exception/Error 문자열 미검출. 정적 화면만으로 실제 이동 부드러움과 단말 드래그 입력까지 검증했다고 판정하지 않는다.
- 독립 규칙 88개 + Unity 저장 3개 통과. 광산 합성 금지, 드래그 취소/중복 방지, 480초 경계, 직진 속도와 역행, 모서리 충돌, 움직이는 목표 경로 갱신 포함.
- 직진 5초 재현: 이전 역행 8회/짧은 이동 15회 → 수정 후 각 0회. 60개 밀집 배치 영웅 진단 통과.
- 남은 확인: 실제 Android 드래그·다중 터치 취소·이동/회전 자연스러움·발열/프레임. v0.4에 기록한 아트 가독성과 상단 안내 잘림은 별도 후속 작업.

## PR 순서

현재 원격 main은 3ee5088(v0.2), 원격 v0.3 브랜치는 fdccce6으로 확인했다. 이미 만든 v0.3 PR을 자동 병합하지 않는다.

1. v0.4: base feature/placement-decks-projectiles → head feature/v04-visuals-navigation
2. v0.5: base feature/v04-visuals-navigation → head feature/v05-drag-merge-movement

각 PR은 앞 버전을 기준으로 작성해 중복 변경을 줄인다. 앞 PR을 병합한 뒤에는 다음 PR의 base와 diff를 다시 확인한다. 원격 게시와 실제 PR 생성 여부는 게시 실행 후 별도로 확인한다.
