namespace Lottie.Database {
    public enum GenericConstraintTypes : uint {
        USER,
        CHANNEL
    }

    public enum ConstraintIntents : uint {
        CHANNELMUTE_GIVE_TEMPORARY,
        CHANNELMUTE_GIVE_PERMANENT,
        CHANNELMUTE_CHECK,
        CHANNELMUTE_REMOVE,

        ROLEPERSIST_GIVE_TEMPORARY,
        ROLEPERSIST_GIVE_PERMANENT,
        ROLEPERSIST_CHECK,
        ROLEPERSIST_REMOVE,

        JAIL_TEMPORARY,
        JAIL_PERMANENT
    }

    public enum PresetMessageTypes : uint {
        CHANNELMUTE_TEMPORARY,
        CHANNELMUTE_PERMANENT,

        JAIL_TEMPORARY,
        JAIL_PERMANENT
    }
}
