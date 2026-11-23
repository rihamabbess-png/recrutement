namespace Recrutement.Models;

public enum ApplicationStatus
{
    EnAnalyse,   // reçue, en attente de l'évaluation IA
    Analysee,    // évaluée par l'IA, en attente du RH
    Entrevue,
    Retenue,
    Refusee
}