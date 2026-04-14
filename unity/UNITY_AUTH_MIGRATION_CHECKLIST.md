# Unity Auth Migration Checklist

## Назначение

Короткий рабочий чеклист для агентов по переходу Unity-авторизации с `Firebase` на backend.

Подробный контекст и пояснения находятся в `UNITY_AUTH_MIGRATION_PLAN.md`.

## Checklist

1. Проверить `OpenAPI` контракт в корне Unity-проекта и подтвердить набор используемых auth/profile routes.
2. Зафиксировать header, из которого Unity получает bearer token.
3. Создать новый Unity backend auth слой вместо `FirebaseManager`.
4. Добавить локальное хранение токена и централизованную подстановку `Authorization: Bearer`.
5. Переписать login на backend auth routes.
6. Переписать register на backend auth routes.
7. Убрать из клиентского flow email verification и все проверки `IsEmailVerified`.
8. Реализовать восстановление сессии при старте приложения по сохраненному токену.
9. Перевести загрузку текущего профиля на backend `profile/me`.
10. Переписать `LobbyManager` так, чтобы он не читал данные из `FirebaseUser`.
11. Переделать смену аватара с URL на upload файла с устройства.
12. Переписать смену пароля на backend route.
13. Переписать signout на backend flow плюс локальную очистку токена.
14. Переназначить все scene bindings и UI callbacks со старого `FirebaseManager` на новый auth/session manager.
15. Проверить, что в сценах не осталось ссылок на Firebase-методы.
16. Удалить `using Firebase` и весь Firebase auth code из Unity scripts.
17. Удалить Firebase SDK, зависимости и resolver artifacts только после полной замены runtime-логики.
18. Обновить `unity/README.md`, убрав Firebase Auth из актуальной документации.
19. Прогнать ручную проверку: register, login, auto-login, profile, avatar upload, change password, signout.
20. Подтвердить, что Unity собирается без Firebase SDK.

## Done Criteria

- В Unity нет runtime-зависимости от `Firebase Auth`.
- Все auth/profile действия идут через backend.
- Bearer token используется как единственный основной механизм авторизации.
- Проект собирается и работает без Firebase SDK.
