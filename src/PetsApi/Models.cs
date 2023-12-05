using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PetsApi;

public record RegisterPetRequest(string Type, string Name);

public record EditPetRequest(string Type, string Name);

public record RegisterPetResponse(Guid PetId);

public record Pet(Guid PetId, string Type, string Name);
