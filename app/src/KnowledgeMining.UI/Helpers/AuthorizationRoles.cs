namespace KnowledgeMining.UI.Helpers
{
    public class AuthorizationRoles
    {
        public static string Required(string? indexName, params Roles[] roles)
        {
            if(string.IsNullOrEmpty(indexName))
                return string.Empty;

            var requiredRoles = new List<string>();
            var lwrIndexName = indexName.ToLower();
            foreach(var role in roles)
            {
                var roleName = role.ToString().ToLower();
                requiredRoles.Add($"{lwrIndexName}.{roleName}");
            }
            requiredRoles.Add($"globaladmin");
            return string.Join(",", requiredRoles);
        }
    }
}
