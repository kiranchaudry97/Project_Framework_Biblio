// Backwards-compatibility wrappers so old references to Biblio_Web.ViewModels keep compiling.
// These simply inherit from the concrete types now located in Biblio_Web.Models.

namespace Biblio_Web.ViewModels
{
    // These wrappers intentionally re-export the types that now live under Biblio_Web.Models.
    // They keep the old namespace usable without moving or renaming existing code.
    public class UserViewModel : Biblio_Web.Models.UserViewModel { }
    public class UserRolesViewModel : Biblio_Web.Models.UserRolesViewModel { }
    public class CreateUserViewModel : Biblio_Web.Models.CreateUserViewModel { }
    public class RoleCheckbox : Biblio_Web.Models.RoleCheckbox { }
    public class AdminEditRolesViewModel : Biblio_Web.Models.AdminEditRolesViewModel { }
}
