# Gravity Runner

Unity/C# 기반의 모바일 원터치 중력 반전 러너 게임입니다.  
직접 개발하여 Google Play에 출시했으며, 출시 이후 기능 추가와 구조 개선을 진행했습니다.

> 본 저장소는 프로젝트 전체 원본이 아닌, 직접 작성한 주요 코드와 구현 구조를 소개하기 위한 포트폴리오용 저장소입니다.

## Project Overview

- Engine: Unity
- Language: C#
- Platform: Android
- Development: Personal Project
- Release: Google Play
- Genre: One-Touch Gravity Runner

플레이어가 화면을 터치하여 중력 방향을 반전시키고 장애물을 피하며 점수를 획득하는 러너 게임입니다.

초기에는 빠른 완성과 실제 출시를 목표로 단순한 구조에서 개발을 시작했으며,  
출시 이후 캐릭터 구매 및 선택, 캐릭터별 능력, 계정 데이터, 랭킹 등 기능을 확장했습니다.

## Key Features

- 중력 반전을 이용한 원터치 플레이
- 무한 스크롤 형태의 플랫폼 생성 및 관리
- 난이도 증가 시스템
- 캐릭터 구매 및 선택 시스템
- 캐릭터별 고유 능력
- Google Play Games 로그인 및 랭킹
- 로컬 / 클라우드 계정 데이터 관리
- 보상형 광고 및 광고 활성화 원격 제어

## Tech Stack

- Unity
- C#
- UniTask
- Addressables
- Google Play Games Services
- Google Mobile Ads
- Firebase Remote Config

## Highlighted Implementation

### Character System

출시 당시에는 하나의 고정 캐릭터만 사용하는 구조였습니다.

이후 업데이트에서 캐릭터 구매, 선택, 게임 내 적용, 캐릭터별 고유 능력이 필요해지면서  
캐릭터 데이터와 UI, 계정 저장 데이터, 게임 플레이 로직을 확장했습니다.

캐릭터별 행동을 `Player` 내부의 조건문으로 직접 처리하지 않고  
`IAbility` 인터페이스를 통해 능력 구현을 분리하고, 선택된 캐릭터에 맞는 능력을 주입하도록 구성했습니다.

이를 통해 캐릭터 콘텐츠와 플레이어의 기본 동작을 분리하여 관리할 수 있도록 개선했습니다.

## Repository Structure

이 저장소에는 포트폴리오 확인에 필요한 주요 C# 코드와 설명 자료만 정리할 예정입니다.

외부 에셋, 빌드 파일, 서비스 설정 파일 및 프로젝트 배포에 필요하지 않은 파일은 포함하지 않습니다.
