namespace Engine.Input.Cursors;

public enum PointerMode
{
    // Обычный видимый курсор.
    Normal,

    // Курсор скрыт, но свободно перемещается.
    Hidden,

    // Курсор скрыт и захвачен окном.
    // Доступно относительное движение.
    Locked
}