ExamMonitor
PC 실습 시험 감독 및 LOG 수집 프로그램
Exam Monitoring & Activity Logging Tool

🇰🇷 소개 (Korean) ====================================================
✅ 특징

무설치, 실행파일 직접 실행, 관리자 권한 불필요
config.txt 하나로 모든 설정 (메모장만 있으면 OK)
Discord 웹훅을 통해

30초마다 실행 중인 프로그램 리스트(백그라운드 포함)
신규 실행 창 / 닫은 창 로그
실시간 스크린샷 + 웹캠 사진 전송


학생이 학번·이름 입력 후 “시험시작” 클릭 → 감독 시작
프로그램 종료 시 마지막 사진 자동 전송
Discord에서 학번·이름·IP·PC 이름으로 검색 가능

📦 설치 및 배포

Discord에서 생성한 webhook_url을 config.txt에 붙여넣기
webhook_url=https://discord.com/api/webhooks/xxxxxxxx


실행파일(ExamMonitor.exe)과 관련 파일을 ZIP으로 묶어 학생에게 배포

⚠️ 시험 전 공지사항

본 프로그램은 시험 감독 목적으로 PC 사용 내역 및 웹캠 자료를 수집합니다.
프로그램을 실행하지 않고 시험을 보는 경우 부정행위로 간주하여 0점 처리됩니다.
시험 종료 시까지 프로그램을 절대 종료하지 말 것
방화벽/안티바이러스 경고 시 반드시 허용할 것

▶ 사용 방법

ZIP 파일 압축 해제 후 ExamMonitor.exe 실행
학번·이름 입력 → 시험시작 버튼 클릭
감독자는 Discord 채널(예: #감독로그)에서 실시간 화면·얼굴 확인 가능
학생이 프로그램 종료 시 → “종료 완료” 메시지 + 마지막 사진 자동 전송


🌍 Introduction (English)=================================================
✅ Features

No installation required, runs without admin privileges
Single config.txt for all settings
Sends via Discord webhook:

Running processes every 30 seconds
Newly opened/closed windows
Real-time screenshots + webcam photos


Student enters ID & name → clicks “Start Exam” → monitoring begins
On program exit, last photo is automatically sent
Search logs by student ID, name, IP, PC name in Discord

📦 Installation & Deployment

Add your Discord webhook_url to config.txt:
webhook_url=https://discord.com/api/webhooks/xxxxxxxx


Package ExamMonitor.exe and related files into a ZIP and distribute to students

⚠️ Important Notices

This software collects PC usage and webcam data for exam monitoring purposes only
Students who do not run the program during the exam will be considered cheating (score = 0)
Do NOT close the program until the exam ends
Allow any firewall/antivirus prompts

▶ How to Use

Extract ZIP → run ExamMonitor.exe
Enter student ID & name → click Start Exam
Instructor monitors via Discord channel (e.g., #exam-log)
When student closes program → “Exit Complete” message + last photo sent


📜 License=========================================================
This project is licensed under the MIT License with Ethical Use Clause.
See ./LICENSE for details.
한국어 안내는 ./LICENSE_KR.txt 참조.