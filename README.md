# Team-Deathmatch
Team-Deathmatch Plugin for SCP:SL (Exiled 기반)
한국어 지원 | Made by a self-taught high school developer
SCP: Secret Laboratory 게임용 Team Deathmatch(TDM) 모드의 핵심 명령어를 구현한 Exiled 플러그인입니다.
서버 운영자가 강제로 TDM을 시작하거나, 유저가 버그로부터 탈출하기 위해 스스로 리스폰할 수 있도록 돕는 기능을 포함합니다.

주요 기능
명령어	설명
.startdm	강제 TDM 시작 – 대기 인원이 10명 이상일 경우, 라운드 중 언제든지 TDM을 강제로 시작
.sr 또는 .SelfRespawn	자기 리스폰 명령어 – 특정 지역(시작 지점)에서 갇히는 버그가 발생했을 때, 사용자가 직접 리스폰 가능 (20초 딜레이)

구현 기술
언어: C#

게임: SCP: Secret Laboratory

서버 API: Exiled

플러그인 구조:

ICommand 인터페이스 기반 명령어 설계

RemoteAdminCommandHandler 사용

싱글턴 기반 Plugin.Instance 접근

Timing.CallDelayed를 활용한 지연 실행 구현


설치 및 사용법
bin/Release에서 .dll 파일 빌드

SCP:SL 서버의 Exiled/Plugins 폴더에 복사

서버 실행 후 콘솔 또는 게임 내 관리자 창에서 다음 명령어 사용:

.startdm

.sr 또는 .SelfRespawn

 관리자 권한이 필요합니다 (RemoteAdmin 권한)

개발 동기
이 플러그인은 고등학생 개발자로서 서버 운영 중 자주 발생하는 TDM 대기 문제와 스폰 버그를 해결하기 위해 직접 제작하였습니다. 실시간 반응성과 사용자 경험을 고려하여 설계되었습니다.

학습 포인트
비동기 함수 실행 (Timing.CallDelayed)

객체 상태 검증 및 안전한 명령어 실행

게임 내 커맨드 UX 설계

Exiled API 구조 이해 및 활용

실제 게임 서버 운영 환경 반영













