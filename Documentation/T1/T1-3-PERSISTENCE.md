# RDA T1-3 Persistence and Settings

Status: implementation complete; final device persistence check belongs to T1-GATE.

## Save model
- JSON file: rda_save_v1.json
- Location: Unity Application.persistentDataPath
- Schema version: 1

## Stored settings
- Master/music/SFX volumes
- Steering sensitivity
- Camera sensitivity
- Language code
- Quality level
- Haptics enabled

## Stored progress
- Selected vehicle ID
- Completed lesson IDs
- Best theory score
- Best practical score

## Safety
- Values are sanitized before use and save.
- Corrupt/missing save data falls back to defaults.
- Save directory is created automatically.
- Writes use a temporary file before replacement.
- Saves on application pause and quit.

## Core integration
RdaGameManager now owns a RdaPersistenceService component automatically.
